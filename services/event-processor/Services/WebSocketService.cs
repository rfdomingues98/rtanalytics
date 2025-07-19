using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EventProcessor.Models;

namespace EventProcessor.Services;

public class WebSocketService : BackgroundService
{
  private readonly ILogger<WebSocketService> _logger;
  private readonly ProcessorConfiguration _config;
  private IHost? _webHost;

  public WebSocketService(ILogger<WebSocketService> logger, IOptions<ProcessorConfiguration> config)
  {
    _logger = logger;
    _config = config.Value;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    try
    {
      _logger.LogInformation("Starting WebSocket service on port 5001");

      _webHost = Host.CreateDefaultBuilder()
          .ConfigureWebHostDefaults(webBuilder =>
          {
            webBuilder.UseUrls("http://localhost:5001");
            webBuilder.Configure(app =>
                  {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                      {
                    endpoints.MapHub<AnalyticsHub>("/analyticsHub");
                  });
              });
          })
          .ConfigureServices(services =>
          {
            services.AddSignalR();
            services.AddCors(options =>
                  {
                options.AddDefaultPolicy(builder =>
                      {
                    builder.AllowAnyOrigin()
                                 .AllowAnyMethod()
                                 .AllowAnyHeader();
                  });
              });
          })
          .Build();

      await _webHost.StartAsync(stoppingToken);
      _logger.LogInformation("WebSocket service started successfully");

      // Keep the service running
      await _webHost.WaitForShutdownAsync(stoppingToken);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error starting WebSocket service");
      throw;
    }
  }

  public override async Task StopAsync(CancellationToken cancellationToken)
  {
    if (_webHost != null)
    {
      await _webHost.StopAsync(cancellationToken);
      _logger.LogInformation("WebSocket service stopped");
    }

    await base.StopAsync(cancellationToken);
  }
}