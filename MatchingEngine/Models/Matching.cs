using Enum;

namespace MatchEngine.Models;

public class Matching
{
    public Ordering? AskOrder { get; set; }

    public Ordering? BidOrder { get; set; }

    public decimal MatchPrice { get; set; }

    public decimal MatchVolume { get; set; }

    public decimal? AskRemainVolume { get; set; }

    public bool Side { get; set; }

    public decimal? AskFee { get; set; }
    
    public decimal? BidCost { get; set; }
    
    public decimal? BidFee { get; set; }

    public TradingType? State { get; set; }

    public long Timestamp { get; set; }
}
