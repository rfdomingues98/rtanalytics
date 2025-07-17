using EventProcessor.Models;
using EventProcessor.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EventProcessor;

public class Program
{
  public static async Task Main(string[] args)
  {
    var host = CreateHostBuilder(args).Build();

    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Starting Event Processor Application");

    // Test Redis connection on startup
    await TestRedisConnectionAsync(host.Services, logger);

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

            // Configure Redis connection
            var processorConfig = context.Configuration.GetSection("ProcessorConfiguration").Get<ProcessorConfiguration>();
            if (processorConfig?.Redis?.Enabled == true)
            {
              services.AddSingleton<IConnectionMultiplexer>(provider =>
              {
                var configuration = provider.GetRequiredService<IOptions<ProcessorConfiguration>>().Value;
                var logger = provider.GetRequiredService<ILogger<Program>>();

                try
                {
                  var options = ConfigurationOptions.Parse(configuration.Redis.ConnectionString);
                  options.AbortOnConnectFail = false;
                  options.ConnectRetry = 3;
                  options.ConnectTimeout = 5000;

                  var multiplexer = ConnectionMultiplexer.Connect(options);

                  multiplexer.ConnectionFailed += (sender, e) =>
                  {
                    logger.LogError("Redis connection failed: {Exception}", e.Exception?.Message);
                  };

                  multiplexer.ConnectionRestored += (sender, e) =>
                  {
                    logger.LogInformation("Redis connection restored");
                  };

                  logger.LogInformation("Redis connection established to {ConnectionString}",
                    configuration.Redis.ConnectionString);

                  return multiplexer;
                }
                catch (Exception ex)
                {
                  logger.LogError(ex, "Failed to connect to Redis at {ConnectionString}",
                    configuration.Redis.ConnectionString);
                  throw;
                }
              });
            }
            else
            {
              // Register a dummy connection multiplexer when Redis is disabled
              services.AddSingleton<IConnectionMultiplexer>(provider =>
              {
                throw new InvalidOperationException("Redis is disabled in configuration");
              });
            }

            // Register services
            services.AddSingleton<IRedisAggregationService, RedisAggregationService>();
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

  private static async Task TestRedisConnectionAsync(IServiceProvider services, ILogger logger)
  {
    try
    {
      var config = services.GetRequiredService<IOptions<ProcessorConfiguration>>().Value;

      if (!config.Redis.Enabled)
      {
        logger.LogWarning("Redis is disabled in configuration. Event aggregation will not be persisted.");
        return;
      }

      var redisService = services.GetRequiredService<IRedisAggregationService>();
      var isConnected = await redisService.IsConnectedAsync();

      if (isConnected)
      {
        var healthInfo = await redisService.GetHealthInfoAsync();
        logger.LogInformation("Redis connection test successful. Version: {Version}, Ping: {Ping}ms",
          healthInfo.GetValueOrDefault("redis_version", "unknown"),
          healthInfo.GetValueOrDefault("ping", "unknown"));
      }
      else
      {
        logger.LogWarning("Redis connection test failed. Event aggregation will not work properly.");
      }
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error testing Redis connection");
    }
  }
}
