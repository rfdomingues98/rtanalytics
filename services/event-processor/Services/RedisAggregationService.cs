using EventProcessor.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace EventProcessor.Services;

public class RedisAggregationService : IRedisAggregationService, IDisposable
{
  private readonly ILogger<RedisAggregationService> _logger;
  private readonly RedisConfiguration _config;
  private readonly IConnectionMultiplexer _redis;
  private readonly IDatabase _database;
  private readonly JsonSerializerOptions _jsonOptions;

  public RedisAggregationService(
      ILogger<RedisAggregationService> logger,
      IOptions<ProcessorConfiguration> config,
      IConnectionMultiplexer redis)
  {
    _logger = logger;
    _config = config.Value.Redis;
    _redis = redis;
    _database = _redis.GetDatabase(_config.Database);

    _jsonOptions = new JsonSerializerOptions
    {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
      WriteIndented = false
    };
  }

  public async Task StoreUserMetricsAsync(UserMetrics userMetrics, CancellationToken cancellationToken = default)
  {
    if (!_config.Enabled) return;

    try
    {
      var key = GetUserMetricsKey(userMetrics.UserId);
      var json = JsonSerializer.Serialize(userMetrics, _jsonOptions);
      var expiry = TimeSpan.FromMinutes(_config.ExpirationMinutes);

      _ = await _database.StringSetAsync(key, json, expiry);

      _logger.LogDebug("Stored user metrics for user {UserId} in Redis", userMetrics.UserId);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to store user metrics for user {UserId}", userMetrics.UserId);
      throw;
    }
  }

  public async Task<UserMetrics?> GetUserMetricsAsync(string userId, CancellationToken cancellationToken = default)
  {
    if (!_config.Enabled) return null;

    try
    {
      var key = GetUserMetricsKey(userId);
      var json = await _database.StringGetAsync(key);

      if (!json.HasValue)
      {
        _logger.LogDebug("User metrics not found for user {UserId}", userId);
        return null;
      }

      var metrics = JsonSerializer.Deserialize<UserMetrics>(json!, _jsonOptions);
      _logger.LogDebug("Retrieved user metrics for user {UserId} from Redis", userId);

      return metrics;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to retrieve user metrics for user {UserId}", userId);
      return null;
    }
  }

  public async Task IncrementUserMetricAsync(string userId, EventData eventData, CancellationToken cancellationToken = default)
  {
    if (!_config.Enabled) return;

    try
    {
      var key = GetUserMetricsKey(userId);
      var expiry = TimeSpan.FromMinutes(_config.ExpirationMinutes);

      // Use Redis hash for atomic operations
      var hashKey = GetUserMetricsHashKey(userId);
      var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(eventData.Timestamp).DateTime;

      // Increment counters atomically
      var transaction = _database.CreateTransaction();

      // Update basic counters
      _ = transaction.HashIncrementAsync(hashKey, "totalEvents", 1);
      _ = transaction.HashSetAsync(hashKey, "lastActivity", timestamp.ToBinary());
      _ = transaction.HashIncrementAsync(hashKey, $"eventType:{eventData.EventType}", 1);

      // Update specific metrics based on event type
      switch (eventData.EventType)
      {
        case EventType.PageView:
          _ = transaction.HashIncrementAsync(hashKey, "pageViews", 1);
          break;
        case EventType.Purchase:
          _ = transaction.HashIncrementAsync(hashKey, "purchases", 1);
          if (eventData.Metadata.TryGetValue("amount", out var amount) &&
              double.TryParse(amount?.ToString(), out var purchaseAmount))
          {
            _ = transaction.HashIncrementAsync(hashKey, "totalSpent", purchaseAmount);
          }
          break;
      }

      _ = transaction.KeyExpireAsync(hashKey, expiry);

      _ = await transaction.ExecuteAsync();

      _logger.LogDebug("Incremented user metrics for user {UserId} and event {EventType}",
          userId, eventData.EventType);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to increment user metrics for user {UserId}", userId);
      throw;
    }
  }

  public async Task StoreEventAggregationAsync(EventAggregation aggregation, CancellationToken cancellationToken = default)
  {
    if (!_config.Enabled) return;

    try
    {
      var key = GetAggregationKey(aggregation.EventType, aggregation.TimeWindow, aggregation.WindowStart);
      var json = JsonSerializer.Serialize(aggregation, _jsonOptions);
      var expiry = TimeSpan.FromMinutes(_config.ExpirationMinutes);

      _ = await _database.StringSetAsync(key, json, expiry);

      _logger.LogDebug("Stored aggregation for {EventType} {TimeWindow} window starting {WindowStart}",
          aggregation.EventType, aggregation.TimeWindow, aggregation.WindowStart);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to store aggregation for {EventType} {TimeWindow}",
          aggregation.EventType, aggregation.TimeWindow);
      throw;
    }
  }

  public async Task<EventAggregation?> GetEventAggregationAsync(EventType eventType, TimeWindow timeWindow, DateTime windowStart, CancellationToken cancellationToken = default)
  {
    if (!_config.Enabled) return null;

    try
    {
      var key = GetAggregationKey(eventType, timeWindow, windowStart);
      var json = await _database.StringGetAsync(key);

      if (!json.HasValue)
      {
        _logger.LogDebug("Aggregation not found for {EventType} {TimeWindow} window starting {WindowStart}",
            eventType, timeWindow, windowStart);
        return null;
      }

      var aggregation = JsonSerializer.Deserialize<EventAggregation>(json!, _jsonOptions);
      _logger.LogDebug("Retrieved aggregation for {EventType} {TimeWindow} window from Redis",
          eventType, timeWindow);

      return aggregation;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to retrieve aggregation for {EventType} {TimeWindow}",
          eventType, timeWindow);
      return null;
    }
  }

  public async Task IncrementEventAggregationAsync(EventData eventData, TimeWindow timeWindow, CancellationToken cancellationToken = default)
  {
    if (!_config.Enabled) return;

    try
    {
      var windowStart = GetWindowStart(timeWindow);
      var key = GetAggregationHashKey(eventData.EventType, timeWindow, windowStart);
      var expiry = TimeSpan.FromMinutes(_config.ExpirationMinutes);

      var transaction = _database.CreateTransaction();

      // Increment count
      _ = transaction.HashIncrementAsync(key, "count", 1);
      _ = transaction.HashSetAsync(key, "lastUpdated", DateTime.UtcNow.ToBinary());

      // Add value if present
      if (eventData.Metadata.TryGetValue("amount", out var amount) &&
          double.TryParse(amount?.ToString(), out var value))
      {
        _ = transaction.HashIncrementAsync(key, "totalValue", value);
      }

      _ = transaction.KeyExpireAsync(key, expiry);

      _ = await transaction.ExecuteAsync();

      _logger.LogDebug("Incremented aggregation for {EventType} {TimeWindow} window",
          eventData.EventType, timeWindow);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to increment aggregation for {EventType} {TimeWindow}",
          eventData.EventType, timeWindow);
      throw;
    }
  }

  public async Task<bool> IsConnectedAsync()
  {
    try
    {
      _ = await _database.PingAsync();
      return _redis.IsConnected;
    }
    catch
    {
      return false;
    }
  }

  public async Task<Dictionary<string, string>> GetHealthInfoAsync()
  {
    var healthInfo = new Dictionary<string, string>();

    try
    {
      var ping = await _database.PingAsync();
      var server = _redis.GetServer(_redis.GetEndPoints().First());
      var info = await server.InfoAsync();

      healthInfo["connected"] = _redis.IsConnected.ToString();
      healthInfo["ping"] = ping.TotalMilliseconds.ToString("F2");

      // Extract Redis version from server info
      var serverGroup = info.FirstOrDefault(g => g.Key == "Server");
      var versionInfo = serverGroup?.FirstOrDefault(kvp => kvp.Key == "redis_version");
      healthInfo["redis_version"] = versionInfo?.Value ?? "unknown";

      healthInfo["database"] = _config.Database.ToString();
      healthInfo["key_prefix"] = _config.KeyPrefix;
    }
    catch (Exception ex)
    {
      healthInfo["error"] = ex.Message;
      healthInfo["connected"] = "false";
    }

    return healthInfo;
  }

  private string GetUserMetricsKey(string userId) => $"{_config.KeyPrefix}user:{userId}";
  private string GetUserMetricsHashKey(string userId) => $"{_config.KeyPrefix}user_hash:{userId}";
  private string GetAggregationKey(EventType eventType, TimeWindow timeWindow, DateTime windowStart) =>
      $"{_config.KeyPrefix}agg:{eventType}:{timeWindow}:{windowStart:yyyy-MM-dd-HH}";
  private string GetAggregationHashKey(EventType eventType, TimeWindow timeWindow, DateTime windowStart) =>
      $"{_config.KeyPrefix}agg_hash:{eventType}:{timeWindow}:{windowStart:yyyy-MM-dd-HH}";

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

  public void Dispose()
  {
    _redis?.Dispose();
  }
}