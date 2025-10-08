using System.Text.Json.Serialization;

namespace MatchEngine.Models;

public class Depth
{
    [JsonPropertyName("prc")]
    public decimal Price { get; set; }

    [JsonPropertyName("vol")]
    public decimal Volume { get; set; }

    [JsonIgnore]
    public long LastChanged { get; set; }
}