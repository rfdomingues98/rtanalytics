using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace EventProcessor.Services;

public class AnalyticsHub : Hub
{
  private readonly ILogger<AnalyticsHub> _logger;

  public AnalyticsHub(ILogger<AnalyticsHub> logger)
  {
    _logger = logger;
  }

  public override async Task OnConnectedAsync()
  {
    _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
    await base.OnConnectedAsync();
  }

  public override async Task OnDisconnectedAsync(Exception? exception)
  {
    _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
    await base.OnDisconnectedAsync(exception);
  }

  public async Task JoinAnalyticsGroup()
  {
    await Groups.AddToGroupAsync(Context.ConnectionId, "analytics");
    _logger.LogInformation("Client {ConnectionId} joined analytics group", Context.ConnectionId);
  }

  public async Task LeaveAnalyticsGroup()
  {
    await Groups.RemoveFromGroupAsync(Context.ConnectionId, "analytics");
    _logger.LogInformation("Client {ConnectionId} left analytics group", Context.ConnectionId);
  }
}