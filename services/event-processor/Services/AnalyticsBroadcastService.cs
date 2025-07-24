using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EventProcessor.Models;
using System.Text.Json;

namespace EventProcessor.Services;

public class AnalyticsBroadcastService : IAnalyticsBroadcastService
{
  private readonly IHubContext<AnalyticsHub> _hubContext;
  private readonly ILogger<AnalyticsBroadcastService> _logger;
  private readonly WebSocketConfiguration _config;

  public AnalyticsBroadcastService(
      IHubContext<AnalyticsHub> hubContext,
      ILogger<AnalyticsBroadcastService> logger,
      IOptions<ProcessorConfiguration> config)
  {
    _hubContext = hubContext;
    _logger = logger;
    _config = config.Value.WebSocket;
  }

  public async Task BroadcastEventAsync(EventData eventData)
  {
    try
    {
      if (!_config.Enabled)
      {
        _logger.LogDebug("WebSocket broadcasting is disabled");
        return;
      }

      var message = new
      {
        type = "event",
        data = eventData,
        timestamp = DateTime.UtcNow
      };

      await _hubContext.Clients.Group("analytics").SendAsync("ReceiveEvent", message);
      _logger.LogDebug("Broadcasted event: {EventType} for user {UserId}",
          eventData.EventType, eventData.UserId);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error broadcasting event");
    }
  }

  public async Task BroadcastAggregationAsync(string key, object aggregationData)
  {
    try
    {
      if (!_config.Enabled)
      {
        _logger.LogDebug("WebSocket broadcasting is disabled");
        return;
      }

      var message = new
      {
        type = "aggregation",
        key = key,
        data = aggregationData,
        timestamp = DateTime.UtcNow
      };

      await _hubContext.Clients.Group("analytics").SendAsync("ReceiveAggregation", message);
      _logger.LogDebug("Broadcasted aggregation for key: {Key}", key);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error broadcasting aggregation");
    }
  }

  public async Task BroadcastHealthUpdateAsync(object healthData)
  {
    try
    {
      if (!_config.Enabled)
      {
        _logger.LogDebug("WebSocket broadcasting is disabled");
        return;
      }

      var message = new
      {
        type = "health",
        data = healthData,
        timestamp = DateTime.UtcNow
      };

      await _hubContext.Clients.Group("analytics").SendAsync("ReceiveHealthUpdate", message);
      _logger.LogDebug("Broadcasted health update");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error broadcasting health update");
    }
  }

  public async Task<bool> IsConnectedAsync()
  {
    try
    {
      // Check if there are any connected clients
      var clients = _hubContext.Clients.All;
      // This is a simple check - in a real implementation you might want to track connected clients
      return true; // For now, assume the service is available if enabled
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error checking WebSocket connection status");
      return false;
    }
  }
}