using Enum;
using MatchEngine.DataStructures;
using MatchEngine.Models;
using MatchingEngine.Extension;

namespace MatchingEngine;

public class MatchingEngine
{
    private readonly int _precision;
    private readonly decimal _stepSize;
    private readonly decimal _makerFeeRate;
    private readonly decimal _takerFeeRate;

    public OrderBook Books { get; }

    public MatchingEngine(decimal stepSize, int pricePrecision = 0, decimal makerFeeRate = 0.001m, decimal takerFeeRate = 0.002m)
    {
        _precision = pricePrecision >= 0 ? pricePrecision : throw new Exception($"Invalid value of {nameof(pricePrecision)}");
        _stepSize = stepSize >= 0 ? stepSize : throw new Exception($"Invalid value of {nameof(stepSize)}");
        _makerFeeRate = makerFeeRate;
        _takerFeeRate = takerFeeRate;

        Books = new OrderBook();
    }

    public MatchState AddOrder(Ordering order, long timestamp, bool isOrderTriggered = false)
    {
        if (order == null)
            return MatchState.OrderInvalid;

        // Handle market orders (Price = 0)
        bool isMarketOrder = order.Price == 0;
        if (isMarketOrder)
        {
            // Set price to guarantee match
            order.Price = order.IsBuy ? decimal.MaxValue : decimal.MinValue;
            // Market orders should use IOC if not already set
            if (order.Condition == OrderCondition.None)
                order.Condition = OrderCondition.IOC;
        }

        order.Status = OrderStatus.Listed;

        // Check conditions
        var state = CheckOrderConditions(order);
        if (state != MatchState.OrderAccepted) return state;

        // For market/IOC orders, try to match immediately without adding to book
        if (order.Condition == OrderCondition.IOC || isMarketOrder)
        {
            var originalVolume = order.Volume;
            var matchResult = TryMatchOrder(order);
            
            // Check if any volume was matched
            var matchedVolume = originalVolume - order.Volume;
            
            // If nothing matched at all
            if (matchedVolume == 0)
            {
                order.Status = OrderStatus.Rejected;
                
                if (isMarketOrder)
                {
                    order.CancelReason = CancelReason.NoLiquidity;
                    return MatchState.MONoLiquidity;
                }
                else
                {
                    order.CancelReason = CancelReason.ImmediateOrCancel;
                    return MatchState.IOCCannotFill;
                }
            }
            
            // Cancel any unfilled portion (partial fill is OK)
            if (!order.IsFilled && order.Volume > 0)
            {
                order.Status = OrderStatus.Cancelled;
                order.CancelReason = CancelReason.ImmediateOrCancel;
            }
            
            return matchResult;
        }

        // Add to order book for other order types
        Books.AddOrder(order);

        // Try to match
        var result = TryMatchOrder(order);
        
        return result;
    }

    public MatchState CancelOrder(Guid id)
    {
        var order = Books.FindOrder(id);
        if (order == null)
            return MatchState.OrderNotExists;

        var removed = Books.RemoveOrder(id);
        if (removed)
        {
            order.Status = OrderStatus.Cancelled;
            order.CancelReason = CancelReason.UserRequested;
            return MatchState.CancelAcepted;
        }

        return MatchState.OrderNotExists;
    }

    private MatchState CheckOrderConditions(Ordering order)
    {
        // BOC condition - order must not match immediately
        if (order.Condition == OrderCondition.BOC)
        {
            var wouldMatch = order.IsBuy 
                ? Books.BestAskPrice.HasValue && Books.BestAskPrice.Value <= order.Price
                : Books.BestBidPrice.HasValue && order.Price <= Books.BestBidPrice.Value;
            
            if (wouldMatch)
            {
                order.Status = OrderStatus.Rejected;
                order.CancelReason = CancelReason.BookOrCancel;
                return MatchState.BOCCannotBook;
            }
        }

        // FOK condition - order must be completely fillable
        if (order.Condition == OrderCondition.FOK && 
            !Books.CheckCanFillOrder(order.IsBuy, order.Volume, order.Price))
        {
            order.Status = OrderStatus.Rejected;
            order.CancelReason = CancelReason.FillOrKill;
            return MatchState.FOKCannotFill;
        }

        return MatchState.OrderAccepted;
    }

    private MatchState TryMatchOrder(Ordering incomingOrder)
    {
        while (!incomingOrder.IsFilled)
        {
            var restingOrder = incomingOrder.IsBuy ? Books.GetBestAskOrder() : Books.GetBestBidOrder();
            if (restingOrder == null) break;

            // Check if orders can match
            if (!CanMatch(incomingOrder, restingOrder)) break;

            // Execute match
            var matchResult = ExecuteMatch(incomingOrder, restingOrder);
            if (matchResult == null) break;
        }

        // Order accepted regardless of whether it matched or went into book
        return MatchState.OrderAccepted;
    }

    private bool CanMatch(Ordering incoming, Ordering resting)
    {
        if (incoming.IsBuy)
            return resting.Price <= incoming.Price;
        else
            return resting.Price >= incoming.Price;
    }

    private Matching? ExecuteMatch(Ordering incomingOrder, Ordering restingOrder)
    {
        var matchPrice = restingOrder.Price;
        var matchVolume = Math.Min(incomingOrder.Volume, restingOrder.Volume);

        // Update volumes
        incomingOrder.Volume -= matchVolume;
        restingOrder.Volume -= matchVolume;

        // Calculate fees
        var incomingFee = CalculateFee(incomingOrder, matchVolume, matchPrice);
        var restingFee = CalculateFee(restingOrder, matchVolume, matchPrice);

        // Update costs
        var cost = matchVolume * matchPrice;
        incomingOrder.Cost += cost;
        restingOrder.Cost += cost;
        incomingOrder.Fee += incomingFee;
        restingOrder.Fee += restingFee;

        // Fill orders if complete
        if (restingOrder.IsFilled)
        {
            Books.FillOrder(restingOrder, matchVolume);
        }

        // Create matching record
        var matching = new Matching
        {
            AskOrder = incomingOrder.IsBuy ? restingOrder.Clone() : incomingOrder.Clone(),
            BidOrder = incomingOrder.IsBuy ? incomingOrder.Clone() : restingOrder.Clone(),
            MatchPrice = matchPrice,
            MatchVolume = matchVolume,
            AskRemainVolume = incomingOrder.IsFilled ? null : incomingOrder.Volume,
            Side = incomingOrder.IsBuy,
            AskFee = incomingOrder.IsBuy ? incomingFee : restingFee,
            BidFee = incomingOrder.IsBuy ? restingFee : incomingFee,
            BidCost = cost,
            State = TradingType.Matched,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        return matching;
    }

    private decimal CalculateFee(Ordering order, decimal volume, decimal price)
    {
        // Incoming order is taker, resting order is maker
        var feeRate = order.IsBuy ? _takerFeeRate : _makerFeeRate;
        
        return order.IsBuy ? 
            Math.Round(volume * feeRate, _precision) : 
            Math.Round(volume * price * feeRate, _precision);
    }
}
