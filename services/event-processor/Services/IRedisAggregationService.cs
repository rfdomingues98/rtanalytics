using EventProcessor.Models;

namespace EventProcessor.Services;

public interface IRedisAggregationService
{
  Task StoreUserMetricsAsync(UserMetrics userMetrics, CancellationToken cancellationToken = default);
  Task<UserMetrics?> GetUserMetricsAsync(string userId, CancellationToken cancellationToken = default);
  Task IncrementUserMetricAsync(string userId, EventData eventData, CancellationToken cancellationToken = default);

  Task StoreEventAggregationAsync(EventAggregation aggregation, CancellationToken cancellationToken = default);
  Task<EventAggregation?> GetEventAggregationAsync(EventType eventType, TimeWindow timeWindow, DateTime windowStart, CancellationToken cancellationToken = default);
  Task IncrementEventAggregationAsync(EventData eventData, TimeWindow timeWindow, CancellationToken cancellationToken = default);

  Task<bool> IsConnectedAsync();
  Task<Dictionary<string, string>> GetHealthInfoAsync();
}