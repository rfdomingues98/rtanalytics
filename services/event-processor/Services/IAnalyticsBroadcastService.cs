using EventProcessor.Models;

namespace EventProcessor.Services;

public interface IAnalyticsBroadcastService
{
  Task BroadcastEventAsync(EventData eventData);
  Task BroadcastAggregationAsync(string key, object aggregationData);
  Task BroadcastHealthUpdateAsync(object healthData);
  Task<bool> IsConnectedAsync();
}