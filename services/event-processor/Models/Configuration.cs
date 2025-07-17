namespace EventProcessor.Models;

public class KafkaConfiguration
{
  public bool Enabled { get; set; } = true;
  public string[] Brokers { get; set; } = ["localhost:9092"];
  public string Topic { get; set; } = "analytics-events";
  public string GroupId { get; set; } = "event-processor";
  public string ClientId { get; set; } = "event-processor-consumer";
  public int SessionTimeoutMs { get; set; } = 30000;
  public int PollTimeoutMs { get; set; } = 100;
  public string AutoOffsetReset { get; set; } = "earliest";
}

public class ProcessorConfiguration
{
  public KafkaConfiguration Kafka { get; set; } = new();
  public int MaxRetries { get; set; } = 3;
  public int RetryDelayMs { get; set; } = 1000;
  public bool EnableBatching { get; set; } = true;
  public int BatchSize { get; set; } = 100;
  public int BatchTimeoutMs { get; set; } = 5000;
}

public class RedisConfiguration
{
  public string ConnectionString { get; set; } = "localhost:6379";
  public string KeyPrefix { get; set; } = "analytics:";
  public int Database { get; set; } = 0;
  public TimeSpan DefaultExpiry { get; set; } = TimeSpan.FromHours(24);
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