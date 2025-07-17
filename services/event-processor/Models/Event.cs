using System.Text.Json.Serialization;

namespace EventProcessor.Models;

public class EventData
{
  [JsonPropertyName("event_type")]
  public EventType EventType { get; set; }
  [JsonPropertyName("user_id")]
  public string UserId { get; set; } = string.Empty;
  [JsonPropertyName("timestamp")]
  public long Timestamp { get; set; }
  [JsonPropertyName("metadata")]
  public Dictionary<string, object?> Metadata { get; set; } = [];
}

public enum EventType
{
  [JsonPropertyName("click")]
  Click,
  [JsonPropertyName("page_view")]
  PageView,
  [JsonPropertyName("purchase")]
  Purchase,
  [JsonPropertyName("add_to_cart")]
  AddToCart,
  [JsonPropertyName("checkout")]
  Checkout,
  [JsonPropertyName("favorite")]
  Favorite,
  [JsonPropertyName("add_review")]
  AddReview,
}

public enum TimeWindow
{
  Hourly,
  Daily
}

public class EventAggregation
{
  public EventType EventType { get; set; }
  public long Count { get; set; }
  public double TotalValue { get; set; }
  public DateTime LastUpdated { get; set; }
  public TimeWindow TimeWindow { get; set; }
  public DateTime WindowStart { get; set; }
}

public class UserMetrics
{
  public string UserId { get; set; } = string.Empty;
  public long TotalEvents { get; set; }
  public long PageViews { get; set; }
  public long Purchases { get; set; }
  public double TotalSpent { get; set; }
  public DateTime LastActivity { get; set; }
  public Dictionary<EventType, long> EventCounts { get; set; } = [];
}