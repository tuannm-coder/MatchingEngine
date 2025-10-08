using System.Text.Json.Serialization;

namespace Models;

public class DepthInfo
{
    [JsonPropertyName("prc")]
    public decimal Price { get; set; }

    [JsonPropertyName("vol")]
    public decimal Volume { get; set; }

    [JsonPropertyName("last")]
    public long LastChanged { get; set; }

    public DepthInfo Clone()
        => new()
        {
            Price = Price,
            Volume = Volume,
            LastChanged = LastChanged
        };
}