using System.Text.Json.Serialization;

namespace Models;

public class MarketInfo
{
    [JsonPropertyName("prc")]
    public decimal Price { get; set; }

    [JsonPropertyName("pct")]
    public decimal Percent { get; set; }

    [JsonPropertyName("open")]
    public decimal OpenPrice { get; set; }

    [JsonPropertyName("close")]
    public decimal ClosePrice { get; set; }

    [JsonPropertyName("min")]
    public decimal MinPrice { get; set; }

    [JsonPropertyName("max")]
    public decimal MaxPrice { get; set; }

    [JsonPropertyName("vol")]
    public decimal TotalVolume { get; set; }

    [JsonIgnore]
    public int LastChanged { get; set; }

    public MarketInfo()
    { }

    public MarketInfo(decimal price, decimal perc, decimal open, decimal close, decimal min, decimal max, decimal vol, int last)
    {
        Price = price;
        Percent = perc;
        OpenPrice = open;
        ClosePrice = close;
        MinPrice = min;
        MaxPrice = max;
        TotalVolume = vol;
        LastChanged = last;
    }

    public MarketInfo Clone()
        => new()
        {
            Price = Price,
            Percent = Percent,
            OpenPrice = OpenPrice,
            ClosePrice = ClosePrice,
            MinPrice = MinPrice,
            MaxPrice = MaxPrice,
            TotalVolume = TotalVolume,
            LastChanged = LastChanged
        };

    public void SetPrice(decimal price, decimal perc, decimal vol)
    {
        Percent = perc;
        Price = ClosePrice = price;
        if (OpenPrice == 0) OpenPrice = price;
        if (MinPrice == 0) MinPrice = price;
        if (MaxPrice == 0) MaxPrice = price;
        //MinPrice = MinPrice == 0 ? price : decimal.Min(MinPrice, price);
        //MaxPrice = decimal.Max(MaxPrice, price);
        TotalVolume += vol;
    }

    public void SetRange(decimal price)
    {
        MinPrice = MinPrice == 0 ? price : decimal.Min(MinPrice, price);
        MaxPrice = decimal.Max(MaxPrice, price);
    }
}