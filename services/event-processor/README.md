# Event Processor Service

A high-performance event processing service that consumes analytics events from Kafka and aggregates them using Redis for real-time analytics.

## Features

- **Kafka Consumer**: Consumes events from Kafka topics
- **Redis Aggregation**: Real-time event aggregation using Redis for persistence
- **User Metrics**: Track user behavior and activity patterns
- **Time-Windowed Aggregation**: Hourly and daily event aggregations
- **Health Monitoring**: Built-in health checks for Redis connectivity
- **Configurable**: Flexible configuration for various environments

## Architecture

```
Kafka Topic → Event Processor → Redis Aggregation → Analytics Dashboard
```

### Components

1. **KafkaConsumerService**: Consumes events from Kafka topics
2. **EventProcessor**: Core processing logic for events
3. **RedisAggregationService**: Handles Redis operations for user metrics and event aggregations
4. **Event Models**: Strongly-typed event data structures

## Redis Data Structure

The service stores two main types of data in Redis:

### User Metrics
- **Key Pattern**: `analytics:user:{userId}`
- **Hash Pattern**: `analytics:user_hash:{userId}` (for atomic operations)
- **Data**: Total events, page views, purchases, total spent, last activity

### Event Aggregations
- **Key Pattern**: `analytics:agg:{eventType}:{timeWindow}:{windowStart}`
- **Hash Pattern**: `analytics:agg_hash:{eventType}:{timeWindow}:{windowStart}` (for atomic operations)
- **Data**: Event counts, total values, time windows (hourly/daily)

## Configuration

### appsettings.json

```json
{
  "ProcessorConfiguration": {
    "Kafka": {
      "Enabled": true,
      "Brokers": ["localhost:9092"],
      "Topic": "analytics-events",
      "GroupId": "event-processor",
      "AutoOffsetReset": "earliest"
    },
    "Redis": {
      "Enabled": true,
      "ConnectionString": "localhost:6379",
      "Database": 0,
      "KeyPrefix": "analytics:",
      "ExpirationMinutes": 1440
    },
    "MaxRetries": 3,
    "EnableBatching": true,
    "BatchSize": 100
  }
}
```

### Environment Variables

You can override configuration using environment variables:

- `ProcessorConfiguration__Redis__ConnectionString`: Redis connection string
- `ProcessorConfiguration__Redis__Enabled`: Enable/disable Redis (true/false)
- `ProcessorConfiguration__Kafka__Brokers__0`: First Kafka broker
- `ProcessorConfiguration__Kafka__Topic`: Kafka topic name

## Running the Service

### Prerequisites

1. **Kafka**: Running on port 9092 (provided by docker-compose)
2. **Redis**: Running on port 6379 (provided by docker-compose)

### Using Docker Compose

1. Start infrastructure services:
```bash
cd ../../  # Go to project root
docker-compose up -d kafka redis
```

2. Build and run the event processor:
```bash
cd services/event-processor
dotnet restore
dotnet build
dotnet run
```

### Development Mode

To disable Redis during development:

```json
{
  "ProcessorConfiguration": {
    "Redis": {
      "Enabled": false
    }
  }
}
```

## Monitoring

### Health Checks

The service includes health monitoring for Redis:

- Connection status
- Ping latency
- Redis version information
- Database and key prefix configuration

### Redis Commander

Access Redis Commander UI at `http://localhost:8082` to monitor stored data and keys.

### Logging

The service provides detailed logging:

- **Info**: Event processing status, Redis connection status
- **Debug**: Detailed event metadata, Redis operation details
- **Error**: Processing failures, Redis connection issues

## Event Types Supported

- `Click`: User click events
- `PageView`: Page view tracking
- `Purchase`: Purchase transactions (includes amount in metadata)
- `AddToCart`: Add to cart actions
- `Checkout`: Checkout process events
- `Favorite`: Favorite/like actions
- `AddReview`: Review submission events

## API Methods

### IEventProcessor

```csharp
// Process incoming events
Task ProcessEventAsync(EventData eventData, CancellationToken cancellationToken = default);

// Get aggregated data for specific event type and time window
Task<EventAggregation> GetAggregationAsync(EventType eventType, TimeWindow timeWindow, CancellationToken cancellationToken = default);

// Get user metrics for specific user
Task<UserMetrics> GetUserMetricsAsync(string userId, CancellationToken cancellationToken = default);
```

### IRedisAggregationService

```csharp
// Store and retrieve user metrics
Task StoreUserMetricsAsync(UserMetrics userMetrics, CancellationToken cancellationToken = default);
Task<UserMetrics?> GetUserMetricsAsync(string userId, CancellationToken cancellationToken = default);

// Store and retrieve event aggregations
Task StoreEventAggregationAsync(EventAggregation aggregation, CancellationToken cancellationToken = default);
Task<EventAggregation?> GetEventAggregationAsync(EventType eventType, TimeWindow timeWindow, DateTime windowStart, CancellationToken cancellationToken = default);

// Atomic increment operations
Task IncrementUserMetricAsync(string userId, EventData eventData, CancellationToken cancellationToken = default);
Task IncrementEventAggregationAsync(EventData eventData, TimeWindow timeWindow, CancellationToken cancellationToken = default);

// Health monitoring
Task<bool> IsConnectedAsync();
Task<Dictionary<string, string>> GetHealthInfoAsync();
```

## Performance Considerations

### Redis Optimizations

1. **Atomic Operations**: Uses Redis transactions for atomic increments
2. **Hash Storage**: Uses Redis hashes for efficient counter operations
3. **Key Expiration**: Automatic key expiration to manage memory usage
4. **Connection Pooling**: Single connection multiplexer for all operations

### Batch Processing

The service supports batch processing of events:

- Configure `EnableBatching: true`
- Set `BatchSize` and `BatchTimeoutMs` for optimal throughput
- Events are processed in batches for better performance

## Troubleshooting

### Redis Connection Issues

1. **Check Redis Service**: Ensure Redis is running on the configured port
2. **Connection String**: Verify the connection string format
3. **Network Access**: Ensure network connectivity to Redis
4. **Authentication**: Check if Redis requires authentication

### Common Issues

1. **Memory Usage**: Monitor Redis memory usage with large event volumes
2. **Key Expiration**: Adjust `ExpirationMinutes` based on retention requirements
3. **Batch Size**: Tune batch size for optimal throughput vs. latency
4. **Log Levels**: Increase log level to Debug for detailed troubleshooting

## Testing Redis Functionality

Use the Redis Commander UI or Redis CLI to inspect stored data:

```bash
# Connect to Redis CLI
docker exec -it <redis-container-name> redis-cli

# List all analytics keys
KEYS analytics:*

# Get user metrics
HGETALL analytics:user_hash:user123

# Get event aggregation
HGETALL analytics:agg_hash:PageView:Hourly:2024-01-15-14
``` 