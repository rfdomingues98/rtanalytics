using EventProcessor.Models;

namespace EventProcessor.Services;

public interface IEventProcessor
{
  Task ProcessEventAsync(EventData eventData, CancellationToken cancellationToken = default);
  Task<EventAggregation> GetAggregationAsync(EventType eventType, TimeWindow timeWindow, CancellationToken cancellationToken = default);
  Task<UserMetrics> GetUserMetricsAsync(string userId, CancellationToken cancellationToken = default);
}