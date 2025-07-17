using EventProcessor.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace EventProcessor.Services;

public class EventProcessor : IEventProcessor
{
  private readonly ILogger<EventProcessor> _logger;
  private readonly ProcessorConfiguration _config;

  // In-memory storage for demo purposes - in production, use Redis/Database
  private readonly ConcurrentDictionary<string, UserMetrics> _userMetrics = new();
  private readonly ConcurrentDictionary<string, EventAggregation> _aggregations = new();

  public EventProcessor(ILogger<EventProcessor> logger, IOptions<ProcessorConfiguration> config)
  {
    _logger = logger;
    _config = config.Value;
  }

  public async Task ProcessEventAsync(EventData eventData, CancellationToken cancellationToken = default)
  {
    _logger.LogInformation("Processing event: {EventType} for user {UserId} at {Timestamp}",
        eventData.EventType, eventData.UserId, DateTimeOffset.FromUnixTimeMilliseconds(eventData.Timestamp));

    try
    {
      // Update user metrics
      await UpdateUserMetricsAsync(eventData, cancellationToken);

      // Update aggregations
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
    await Task.CompletedTask; // Simulate async operation

    var key = $"{eventType}_{timeWindow}_{GetWindowStart(timeWindow)}";

    if (_aggregations.TryGetValue(key, out var aggregation))
    {
      return aggregation;
    }

    // Return default aggregation if not found
    return new EventAggregation
    {
      EventType = eventType,
      Count = 0,
      TotalValue = 0,
      LastUpdated = DateTime.UtcNow,
      TimeWindow = timeWindow,
      WindowStart = GetWindowStart(timeWindow)
    };
  }

  public async Task<UserMetrics> GetUserMetricsAsync(string userId, CancellationToken cancellationToken = default)
  {
    await Task.CompletedTask; // Simulate async operation

    if (_userMetrics.TryGetValue(userId, out var metrics))
    {
      return metrics;
    }

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

  private async Task UpdateUserMetricsAsync(EventData eventData, CancellationToken cancellationToken)
  {
    await Task.CompletedTask; // Simulate async operation

    var metrics = _userMetrics.GetOrAdd(eventData.UserId, _ => new UserMetrics
    {
      UserId = eventData.UserId,
      EventCounts = new Dictionary<EventType, long>()
    });

    // Update metrics
    metrics.TotalEvents++;
    metrics.LastActivity = DateTimeOffset.FromUnixTimeMilliseconds(eventData.Timestamp).DateTime;

    // Update event type counts
    metrics.EventCounts[eventData.EventType] = metrics.EventCounts.GetValueOrDefault(eventData.EventType, 0) + 1;

    // Update specific metrics based on event type
    switch (eventData.EventType)
    {
      case EventType.PageView:
        metrics.PageViews++;
        break;
      case EventType.Purchase:
        metrics.Purchases++;
        if (eventData.Metadata.TryGetValue("amount", out var amount) &&
            double.TryParse(amount?.ToString(), out var purchaseAmount))
        {
          metrics.TotalSpent += purchaseAmount;
        }
        break;
    }

    _logger.LogDebug("Updated metrics for user {UserId}: {TotalEvents} total events",
        eventData.UserId, metrics.TotalEvents);
  }

  private async Task UpdateAggregationsAsync(EventData eventData, CancellationToken cancellationToken)
  {
    await Task.CompletedTask; // Simulate async operation

    // Update hourly aggregation
    await UpdateAggregationForWindow(eventData, TimeWindow.Hourly, cancellationToken);

    // Update daily aggregation
    await UpdateAggregationForWindow(eventData, TimeWindow.Daily, cancellationToken);
  }

  private async Task UpdateAggregationForWindow(EventData eventData, TimeWindow timeWindow, CancellationToken cancellationToken)
  {
    await Task.CompletedTask; // Simulate async operation

    var windowStart = GetWindowStart(timeWindow);
    var key = $"{eventData.EventType}_{timeWindow}_{windowStart:yyyy-MM-dd-HH}";

    var aggregation = _aggregations.GetOrAdd(key, _ => new EventAggregation
    {
      EventType = eventData.EventType,
      Count = 0,
      TotalValue = 0,
      TimeWindow = timeWindow,
      WindowStart = windowStart,
      LastUpdated = DateTime.UtcNow
    });

    // Update aggregation
    aggregation.Count++;
    aggregation.LastUpdated = DateTime.UtcNow;

    // Add value if present in metadata
    if (eventData.Metadata.TryGetValue("amount", out var amount) &&
        double.TryParse(amount?.ToString(), out var value))
    {
      aggregation.TotalValue += value;
    }

    _logger.LogDebug("Updated {TimeWindow} aggregation for {EventType}: {Count} events",
        timeWindow, eventData.EventType, aggregation.Count);
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