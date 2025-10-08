using MatchEngine.Models;

namespace MatchEngine.DataStructures;

public class OrderBook
{
    private readonly IndexedHeap<decimal> _bidHeap;      // Max heap for bids
    private readonly IndexedHeap<decimal> _askHeap;     // Min heap for asks
    private readonly Dictionary<decimal, BidPriceLevel> _bidLevels;
    private readonly Dictionary<decimal, AskPriceLevel> _askLevels;
    private readonly Dictionary<Guid, OrderNode> _orderIndex;
    
    // Cache for sorted iteration (MUCH faster than GetSortedElements!)
    private List<decimal>? _sortedBidPrices;
    private List<decimal>? _sortedAskPrices;
    private bool _bidCacheDirty = true;
    private bool _askCacheDirty = true;

    public OrderBook()
    {
        _bidHeap = new IndexedHeap<decimal>(true);   // Max heap
        _askHeap = new IndexedHeap<decimal>(false);  // Min heap
        _bidLevels = new Dictionary<decimal, BidPriceLevel>();
        _askLevels = new Dictionary<decimal, AskPriceLevel>();
        _orderIndex = new Dictionary<Guid, OrderNode>();
    }

    // Properties
    public decimal? BestBidPrice => _bidHeap.IsEmpty ? null : _bidHeap.Peek();
    public decimal? BestAskPrice => _askHeap.IsEmpty ? null : _askHeap.Peek();
    public decimal? BestBidVolume => BestBidPrice.HasValue ? _bidLevels[BestBidPrice.Value].TotalVolume : null;
    public decimal? BestAskVolume => BestAskPrice.HasValue ? _askLevels[BestAskPrice.Value].TotalVolume : null;

    public int BidLevelCount => _bidLevels.Count;
    public int AskLevelCount => _askLevels.Count;
    public int TotalOrders => _orderIndex.Count;

    // Public methods
    public void AddOrder(Ordering order)
    {
        var orderNode = new OrderNode(order);
        _orderIndex[order.OrdId] = orderNode;

        if (order.IsBuy)
        {
            AddToBidLevel(order, orderNode);
        }
        else
        {
            AddToAskLevel(order, orderNode);
        }
    }

    public bool RemoveOrder(Guid orderId)
    {
        if (!_orderIndex.TryGetValue(orderId, out var orderNode))
            return false;

        var order = orderNode.Order;
        if (order.IsBuy)
        {
            RemoveFromBidLevel(order);
        }
        else
        {
            RemoveFromAskLevel(order);
        }

        _orderIndex.Remove(orderId);
        return true;
    }

    public Ordering? FindOrder(Guid orderId)
    {
        return _orderIndex.TryGetValue(orderId, out var node) ? node.Order : null;
    }

    public Ordering? GetBestBidOrder()
    {
        if (!BestBidPrice.HasValue) return null;
        return _bidLevels[BestBidPrice.Value].Orders.First?.Value;
    }

    public Ordering? GetBestAskOrder()
    {
        if (!BestAskPrice.HasValue) return null;
        return _askLevels[BestAskPrice.Value].Orders.First?.Value;
    }

    public bool FillOrder(Ordering order, decimal volume)
    {
        if (!_orderIndex.TryGetValue(order.OrdId, out var orderNode))
            return false;

        PriceLevel priceLevel = order.IsBuy ? _bidLevels[order.Price] : _askLevels[order.Price];
        
        // Update PriceLevel's total volume
        priceLevel.TotalVolume -= volume;
        priceLevel.LastUpdated = DateTime.UtcNow;

        // If order is fully filled, remove it from book
        if (order.IsFilled)
        {
            RemoveOrder(order.OrdId);
            return true;
        }

        return false;
    }

    public Depth? GetDepth(decimal price)
    {
        if (_bidLevels.TryGetValue(price, out var bidLevel))
        {
            return new Depth
            {
                Price = price,
                Volume = bidLevel.TotalVolume,
                LastChanged = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }

        if (_askLevels.TryGetValue(price, out var askLevel))
        {
            return new Depth
            {
                Price = price,
                Volume = askLevel.TotalVolume,
                LastChanged = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }

        return null;
    }

    public bool CheckCanFillOrder(bool isBuy, decimal volume, decimal limitPrice)
    {
        decimal cumulativeVolume = 0;
        
        if (isBuy)
        {
            // For buy orders, check ask levels (ascending price order)
            var askPrices = GetSortedAskPrices();
            foreach (var price in askPrices)
            {
                if (limitPrice > 0 && price > limitPrice) break;
                if (cumulativeVolume >= volume) break;
                
                cumulativeVolume += _askLevels[price].TotalVolume;
            }
        }
        else
        {
            // For sell orders, check bid levels (descending price order)
            var bidPrices = GetSortedBidPrices();
            foreach (var price in bidPrices)
            {
                if (limitPrice > 0 && price < limitPrice) break;
                if (cumulativeVolume >= volume) break;
                
                cumulativeVolume += _bidLevels[price].TotalVolume;
            }
        }
        
        return cumulativeVolume >= volume;
    }
    
    // Get sorted prices with caching (FAST!)
    private List<decimal> GetSortedBidPrices()
    {
        if (_bidCacheDirty || _sortedBidPrices == null)
        {
            _sortedBidPrices = _bidLevels.Keys.OrderByDescending(p => p).ToList();
            _bidCacheDirty = false;
        }
        return _sortedBidPrices;
    }
    
    private List<decimal> GetSortedAskPrices()
    {
        if (_askCacheDirty || _sortedAskPrices == null)
        {
            _sortedAskPrices = _askLevels.Keys.OrderBy(p => p).ToList();
            _askCacheDirty = false;
        }
        return _sortedAskPrices;
    }
    
    // Invalidate cache when structure changes
    private void InvalidateBidCache() => _bidCacheDirty = true;
    private void InvalidateAskCache() => _askCacheDirty = true;

    // Private methods
    private void AddToBidLevel(Ordering order, OrderNode orderNode)
    {
        if (!_bidLevels.TryGetValue(order.Price, out var level))
        {
            level = new BidPriceLevel(order.Price);
            _bidLevels[order.Price] = level;
            _bidHeap.Insert(order.Price);
            InvalidateBidCache();  // Invalidate cache when adding new price level
        }

        level.AddOrder(order, orderNode);
    }

    private void AddToAskLevel(Ordering order, OrderNode orderNode)
    {
        if (!_askLevels.TryGetValue(order.Price, out var level))
        {
            level = new AskPriceLevel(order.Price);
            _askLevels[order.Price] = level;
            _askHeap.Insert(order.Price);
            InvalidateAskCache();  // Invalidate cache when adding new price level
        }

        level.AddOrder(order, orderNode);
    }

    private void RemoveFromBidLevel(Ordering order)
    {
        if (!_bidLevels.TryGetValue(order.Price, out var level))
            return;

        level.RemoveOrder(order);
        if (level.IsEmpty)
        {
            _bidLevels.Remove(order.Price);
            _bidHeap.Remove(order.Price);
            InvalidateBidCache();  // Invalidate cache when removing price level
        }
    }

    private void RemoveFromAskLevel(Ordering order)
    {
        if (!_askLevels.TryGetValue(order.Price, out var level))
            return;

        level.RemoveOrder(order);
        if (level.IsEmpty)
        {
            _askLevels.Remove(order.Price);
            _askHeap.Remove(order.Price);
            InvalidateAskCache();  // Invalidate cache when removing price level
        }
    }
}

// Supporting classes
public class OrderNode
{
    public Ordering Order { get; set; }
    public LinkedListNode<Ordering>? ListNode { get; set; }

    public OrderNode(Ordering order)
    {
        Order = order;
    }
}

public abstract class PriceLevel
{
    public decimal Price { get; set; }
    public decimal TotalVolume { get; set; }
    public LinkedList<Ordering> Orders { get; set; }
    public DateTime LastUpdated { get; set; }

    protected PriceLevel(decimal price)
    {
        Price = price;
        Orders = new LinkedList<Ordering>();
        TotalVolume = 0;
        LastUpdated = DateTime.UtcNow;
    }

    public bool IsEmpty => Orders.Count == 0;

    public virtual void AddOrder(Ordering order, OrderNode orderNode)
    {
        var node = Orders.AddLast(order);
        orderNode.ListNode = node;
        TotalVolume += order.Volume;
        LastUpdated = DateTime.UtcNow;
    }

    public virtual void RemoveOrder(Ordering order)
    {
        // Find and remove the order from the linked list
        var node = Orders.First;
        while (node != null)
        {
            if (node.Value.OrdId == order.OrdId)
            {
                Orders.Remove(node);
                TotalVolume -= order.Volume;
                LastUpdated = DateTime.UtcNow;
                break;
            }
            node = node.Next;
        }
    }
}

public class BidPriceLevel : PriceLevel
{
    public BidPriceLevel(decimal price) : base(price) { }
}

public class AskPriceLevel : PriceLevel
{
    public AskPriceLevel(decimal price) : base(price) { }
}
