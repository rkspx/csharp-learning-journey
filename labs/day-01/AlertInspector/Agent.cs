using System.Text.Json.Serialization;

namespace AlertInspector;

public class Agent
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}