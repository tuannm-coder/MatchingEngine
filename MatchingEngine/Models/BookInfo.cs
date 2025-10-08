using System.Text.Json.Serialization;

namespace Models;

public class BookInfo
{
    [JsonPropertyName("sym")]
    public string Symbol { get; set; }

    [JsonPropertyName("side")]
    public bool Side { get; set; }

    [JsonPropertyName("prc")]
    public decimal Price { get; set; }

    [JsonPropertyName("vol")]
    public decimal Volume { get; set; }

    [JsonPropertyName("last")]
    public long LastChanged { get; set; }

    public BookInfo Clone()
        => new()
        {
            Symbol = Symbol,
            Side = Side,
            Price = Price,
            Volume = Volume,
            LastChanged = LastChanged
        };

    public DepthInfo ToDepth()
        => new()
        {
            Price = Price,
            Volume = Volume,
            LastChanged = LastChanged
        };
}