using Confluent.Kafka;
using EventProcessor.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventProcessor.Services;

public class KafkaConsumerService : BackgroundService
{
  private readonly ILogger<KafkaConsumerService> _logger;
  private readonly ProcessorConfiguration _config;
  private readonly IEventProcessor _eventProcessor;
  private IConsumer<string, string>? _consumer;

  public KafkaConsumerService(
      ILogger<KafkaConsumerService> logger,
      IOptions<ProcessorConfiguration> config,
      IEventProcessor eventProcessor)
  {
    _logger = logger;
    _config = config.Value;
    _eventProcessor = eventProcessor;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    if (!_config.Kafka.Enabled)
    {
      _logger.LogInformation("Kafka consumer is disabled");
      return;
    }

    _logger.LogInformation("Starting Kafka consumer service");

    var consumerConfig = new ConsumerConfig
    {
      BootstrapServers = string.Join(",", _config.Kafka.Brokers),
      GroupId = _config.Kafka.GroupId,
      ClientId = _config.Kafka.ClientId,
      AutoOffsetReset = _config.Kafka.AutoOffsetReset == "earliest" ? AutoOffsetReset.Earliest : AutoOffsetReset.Latest,
      SessionTimeoutMs = _config.Kafka.SessionTimeoutMs,
      EnableAutoCommit = false, // Manual commit for better control
      EnablePartitionEof = false,
      AllowAutoCreateTopics = true
    };

    _consumer = new ConsumerBuilder<string, string>(consumerConfig)
        .SetErrorHandler((_, e) => _logger.LogError("Consumer error: {Error}", e.Reason))
        .SetStatisticsHandler((_, json) => _logger.LogDebug("Consumer stats: {Stats}", json))
        .Build();

    try
    {
      _consumer.Subscribe(_config.Kafka.Topic);
      _logger.LogInformation("Subscribed to topic: {Topic}", _config.Kafka.Topic);

      while (!stoppingToken.IsCancellationRequested)
      {
        try
        {
          var result = _consumer.Consume(TimeSpan.FromMilliseconds(_config.Kafka.PollTimeoutMs));

          if (result != null)
          {
            await ProcessMessage(result, stoppingToken);

            // Commit the offset after successful processing
            _consumer.Commit(result);
          }
        }
        catch (ConsumeException ex)
        {
          _logger.LogError(ex, "Error consuming message: {Error}", ex.Error.Reason);
        }
        catch (OperationCanceledException)
        {
          _logger.LogInformation("Kafka consumer operation was cancelled");
          break;
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Unexpected error in Kafka consumer");
          await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Fatal error in Kafka consumer service");
    }
    finally
    {
      _consumer?.Close();
      _consumer?.Dispose();
    }
  }

  private async Task ProcessMessage(ConsumeResult<string, string> result, CancellationToken cancellationToken)
  {
    try
    {
      _logger.LogDebug("Processing message from partition {Partition}, offset {Offset}",
          result.Partition.Value, result.Offset.Value);

      // Deserialize the event data
      var options = new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
      };
      var eventData = JsonSerializer.Deserialize<EventData>(result.Message.Value, options);

      if (eventData == null)
      {
        _logger.LogWarning("Failed to deserialize event data from message: {Message}", result.Message.Value);
        return;
      }

      // Process the event
      await _eventProcessor.ProcessEventAsync(eventData, cancellationToken);

      _logger.LogInformation("Successfully processed event {EventType} for user {UserId}",
          eventData.EventType, eventData.UserId);
    }
    catch (JsonException ex)
    {
      _logger.LogError(ex, "Failed to deserialize message: {Message}", result.Message.Value);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error processing message: {Message}", result.Message.Value);
      throw; // Re-throw to handle retry logic at a higher level
    }
  }

  public override async Task StopAsync(CancellationToken cancellationToken)
  {
    _logger.LogInformation("Stopping Kafka consumer service");

    _consumer?.Close();

    await base.StopAsync(cancellationToken);
  }
}