namespace EventProcessor.Models;

public class ProcessorConfiguration
{
  public KafkaConfiguration Kafka { get; set; } = new();
  public RedisConfiguration Redis { get; set; } = new();
  public SqlServerConfiguration SqlServer { get; set; } = new();
  public ProcessingConfiguration Processing { get; set; } = new();
}

public class KafkaConfiguration
{
  public string BootstrapServers { get; set; } = "localhost:9092";
  public string Topic { get; set; } = "analytics-events";
  public string GroupId { get; set; } = "event-processor";
  public string ClientId { get; set; } = "event-processor-consumer";
  public bool AutoOffsetReset { get; set; } = true;
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