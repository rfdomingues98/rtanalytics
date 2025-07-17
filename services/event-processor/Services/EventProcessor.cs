using EventProcessor.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventProcessor.Services;

public class EventProcessor : IEventProcessor
{
  private readonly ILogger<EventProcessor> _logger;
  private readonly ProcessorConfiguration _config;
  private readonly IRedisAggregationService _redisService;

  public EventProcessor(
    ILogger<EventProcessor> logger,
    IOptions<ProcessorConfiguration> config,
    IRedisAggregationService redisService)
  {
    _logger = logger;
    _config = config.Value;
    _redisService = redisService;
  }

  public async Task ProcessEventAsync(EventData eventData, CancellationToken cancellationToken = default)
  {
    _logger.LogInformation("Processing event: {EventType} for user {UserId} at {Timestamp}",
        eventData.EventType, eventData.UserId, DateTimeOffset.FromUnixTimeMilliseconds(eventData.Timestamp));

    try
    {
      // Check Redis connection
      if (_config.Redis.Enabled && !await _redisService.IsConnectedAsync())
      {
        _logger.LogWarning("Redis is not connected, skipping aggregation storage");
      }

      // Update user metrics in Redis
      await UpdateUserMetricsAsync(eventData, cancellationToken);

      // Update aggregations in Redis
      await UpdateAggregationsAsync(eventData, cancellationToken);

      // Log additional event details
      if (eventData.Metadata.Count > 0)
      {
        _logger.LogDebug("Event metadata: {Metadata}",
            string.Join(", ", eventData.Metadata.Select(kvp => $"{kvp.Key}: {kvp.Value}")));
      }

      // Simulate processing delay (remove in production)
      await Task.Delay(50, cancellationToken);

      _logger.LogInformation("Successfully processed event {EventType} for user {UserId}",
          eventData.EventType, eventData.UserId);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error processing event {EventType} for user {UserId}",
          eventData.EventType, eventData.UserId);
      throw;
    }
  }

  public async Task<EventAggregation> GetAggregationAsync(EventType eventType, TimeWindow timeWindow, CancellationToken cancellationToken = default)
  {
    try
    {
      var windowStart = GetWindowStart(timeWindow);

      // Try to get from Redis first
      if (_config.Redis.Enabled)
      {
        var redisAggregation = await _redisService.GetEventAggregationAsync(eventType, timeWindow, windowStart, cancellationToken);
        if (redisAggregation != null)
        {
          _logger.LogDebug("Retrieved aggregation from Redis for {EventType} {TimeWindow}", eventType, timeWindow);
          return redisAggregation;
        }
      }

      _logger.LogDebug("Aggregation not found for {EventType} {TimeWindow}, returning default", eventType, timeWindow);

      // Return default aggregation if not found
      return new EventAggregation
      {
        EventType = eventType,
        Count = 0,
        TotalValue = 0,
        LastUpdated = DateTime.UtcNow,
        TimeWindow = timeWindow,
        WindowStart = windowStart
      };
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving aggregation for {EventType} {TimeWindow}", eventType, timeWindow);
      throw;
    }
  }

  public async Task<UserMetrics> GetUserMetricsAsync(string userId, CancellationToken cancellationToken = default)
  {
    try
    {
      // Try to get from Redis first
      if (_config.Redis.Enabled)
      {
        var redisMetrics = await _redisService.GetUserMetricsAsync(userId, cancellationToken);
        if (redisMetrics != null)
        {
          _logger.LogDebug("Retrieved user metrics from Redis for user {UserId}", userId);
          return redisMetrics;
        }
      }

      _logger.LogDebug("User metrics not found for user {UserId}, returning default", userId);

      // Return default metrics if user not found
      return new UserMetrics
      {
        UserId = userId,
        TotalEvents = 0,
        PageViews = 0,
        Purchases = 0,
        TotalSpent = 0,
        LastActivity = DateTime.UtcNow,
        EventCounts = new Dictionary<EventType, long>()
      };
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving user metrics for user {UserId}", userId);
      throw;
    }
  }

  private async Task UpdateUserMetricsAsync(EventData eventData, CancellationToken cancellationToken)
  {
    try
    {
      if (_config.Redis.Enabled)
      {
        // Use Redis for atomic increment operations
        await _redisService.IncrementUserMetricAsync(eventData.UserId, eventData, cancellationToken);
        _logger.LogDebug("Updated user metrics in Redis for user {UserId}", eventData.UserId);
      }
      else
      {
        _logger.LogWarning("Redis is disabled, user metrics will not be persisted");
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to update user metrics for user {UserId}", eventData.UserId);
      throw;
    }
  }

  private async Task UpdateAggregationsAsync(EventData eventData, CancellationToken cancellationToken)
  {
    try
    {
      if (_config.Redis.Enabled)
      {
        // Update hourly aggregation
        await _redisService.IncrementEventAggregationAsync(eventData, TimeWindow.Hourly, cancellationToken);

        // Update daily aggregation
        await _redisService.IncrementEventAggregationAsync(eventData, TimeWindow.Daily, cancellationToken);

        _logger.LogDebug("Updated aggregations in Redis for event {EventType}", eventData.EventType);
      }
      else
      {
        _logger.LogWarning("Redis is disabled, event aggregations will not be persisted");
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to update aggregations for event {EventType}", eventData.EventType);
      throw;
    }
  }

  private static DateTime GetWindowStart(TimeWindow timeWindow)
  {
    var now = DateTime.UtcNow;
    return timeWindow switch
    {
      TimeWindow.Hourly => new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc),
      TimeWindow.Daily => new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc),
      _ => now
    };
  }
}