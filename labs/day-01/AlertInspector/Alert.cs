using System.Text.Json.Serialization;

namespace AlertInspector;

public class Alert
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("agent")]
    public Agent? Agent { get; set; }

    [JsonPropertyName("rule")]
    public Rule? Rule { get; set; }
}