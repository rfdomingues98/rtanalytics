using EventProcessor.Models;
using EventProcessor.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventProcessor;

public class Program
{
  public static async Task Main(string[] args)
  {
    var host = CreateHostBuilder(args).Build();

    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Starting Event Processor Application");

    try
    {
      await host.RunAsync();
    }
    catch (Exception ex)
    {
      logger.LogCritical(ex, "Application terminated unexpectedly");
      throw;
    }
    finally
    {
      logger.LogInformation("Event Processor Application stopped");
    }
  }

  private static IHostBuilder CreateHostBuilder(string[] args) =>
      Host.CreateDefaultBuilder(args)
          .ConfigureAppConfiguration((context, config) =>
          {
            config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            config.AddEnvironmentVariables();
            config.AddCommandLine(args);
          })
          .ConfigureServices((context, services) =>
          {
            // Configure ProcessorConfiguration
            services.Configure<ProcessorConfiguration>(
                  context.Configuration.GetSection("ProcessorConfiguration"));

            // Register services
            services.AddSingleton<IEventProcessor, Services.EventProcessor>();
            services.AddHostedService<KafkaConsumerService>();

            // Configure logging
            services.AddLogging(builder =>
              {
                builder.AddConsole();
                builder.AddDebug();
              });
          })
          .ConfigureLogging((context, logging) =>
          {
            logging.ClearProviders();
            logging.AddConsole();
            logging.AddDebug();
          });
}
