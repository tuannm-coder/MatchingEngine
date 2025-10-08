using Enum;

namespace MatchEngine.Models;

public class Transaction
{
    public Ordering? Order { get; set; }

    public decimal? Amount { get; set; }

    public decimal? Locked { get; set; }

    public decimal? Matched { get; set; }

    public decimal? Cost { get; set; }

    public decimal? Fee { get; set; }

    public OrderStatus Status { get; set; }

    public long Timestamp { get; set; }
}