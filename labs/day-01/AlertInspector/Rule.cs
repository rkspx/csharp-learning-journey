using System.Text.Json.Serialization;

namespace AlertInspector;

public class Rule
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("level")]
    public int? Level { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}