using System.ComponentModel.DataAnnotations;

namespace EventProcessor.Models;

public class KafkaConfiguration
{
  public bool Enabled { get; set; } = true;
  [Required]
  public List<string> Brokers { get; set; } = [];
  [Required]
  public string Topic { get; set; } = string.Empty;
  [Required]
  public string GroupId { get; set; } = string.Empty;
  [Required]
  public string ClientId { get; set; } = string.Empty;
  public int SessionTimeoutMs { get; set; } = 30000;
  public int PollTimeoutMs { get; set; } = 100;
  public string AutoOffsetReset { get; set; } = "earliest";
}

public class ProcessorConfiguration
{
  public KafkaConfiguration Kafka { get; set; } = new();
  public RedisConfiguration Redis { get; set; } = new();
  public int MaxRetries { get; set; } = 3;
  public int RetryDelayMs { get; set; } = 1000;
  public bool EnableBatching { get; set; } = true;
  public int BatchSize { get; set; } = 100;
  public int BatchTimeoutMs { get; set; } = 5000;
}

public class RedisConfiguration
{
  public bool Enabled { get; set; } = true;
  [Required]
  public string ConnectionString { get; set; } = "localhost:6379";
  public int Database { get; set; } = 0;
  public string KeyPrefix { get; set; } = "analytics:";
  public int ExpirationMinutes { get; set; } = 1440; // 24 hours
}

public class SqlServerConfiguration
{
  public string ConnectionString { get; set; } = "Server=localhost,1433;Database=Analytics;User Id=sa;Password=StrongPassword123!;TrustServerCertificate=true;";
  public string EventsTable { get; set; } = "RawEvents";
  public int BatchSize { get; set; } = 100;
  public TimeSpan BatchTimeout { get; set; } = TimeSpan.FromSeconds(5);
}

public class ProcessingConfiguration
{
  public int MaxConcurrentMessages { get; set; } = 10;
  public TimeSpan ProcessingTimeout { get; set; } = TimeSpan.FromSeconds(30);
  public bool EnableHealthChecks { get; set; } = true;
  public TimeSpan AggregationWindowSize { get; set; } = TimeSpan.FromMinutes(5);
}