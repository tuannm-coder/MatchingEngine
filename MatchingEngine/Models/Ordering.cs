using Enum;

namespace MatchEngine.Models;

public class Ordering()
{
    #region Properties
    public Guid OrdId { get; set; }

    public long OrdNo { get; set; }

    public string? OrdCode { get; set; }

    public string? Symbol { get; set; }

    public string User { get; set; } = "";

    public bool IsBuy { get; set; }

    public decimal Volume { get; set; }

    public decimal Price { get; set; }

    public int CancelOn { get; set; }

    public decimal Cost { get; set; }

    public decimal Fee { get; set; }

    public int FeeId { get; set; }

    public decimal TipVolume { get; set; }

    public decimal TotalVolume { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal StopPrice { get; set; }

    public OrderCondition Condition { get; set; }

    public SelfMatchAction SelfMatchAction { get; set; }

    public OrderStatus Status { get; set; }

    public CancelReason? CancelReason { get; set; }

    public long Timestamp { get; set; }

    public bool IsFilled => Volume == 0;

    public bool IsStop => StopPrice > 0;

    public bool IsTip => TipVolume > 0 && TotalVolume > 0;
    #endregion

    #region Override Implementation
    public override bool Equals(object? obj)
        => obj != null && obj is Ordering order ? OrdId == order.OrdId : base.Equals(obj);

    public override int GetHashCode() => OrdId.GetHashCode();
    #endregion

    public bool Decrease(decimal decrement)
    {
        var volume = IsTip ? Volume + TotalVolume : Volume;
        if (volume <= decrement || decrement <= 0)
            return false;

        if (IsTip)
        {
            var t = decimal.Min(decrement, TotalVolume);
            TotalVolume -= t;
            decrement -= t;
        }

        Volume -= decrement;
        return true;
    }

    public Ordering Clone()
        => new()
        {
            IsBuy = IsBuy,
            Price = Price,
            OrdId = OrdId,
            OrdNo = OrdNo,
            Symbol = Symbol,
            OrdCode = OrdCode,
            Volume = Volume,
            CancelOn = CancelOn,
            Cost = Cost,
            Fee = Fee,
            TipVolume = TipVolume,
            TotalVolume = TotalVolume,
            TotalAmount = TotalAmount,
            User = User,
            FeeId = FeeId,
            Condition = Condition,
            StopPrice = StopPrice,
            SelfMatchAction = SelfMatchAction,
            Status = Status,
            CancelReason = CancelReason,
            Timestamp = Timestamp
        };
}