# MatchingEngine

A high-performance order matching engine implementation in C# for trading systems. This engine implements price-time priority matching similar to what is used in stock exchanges and cryptocurrency trading platforms.

## 🚀 Features

- **High-Performance Order Matching**: O(log n) time complexity for order operations using indexed heaps
- **Price-Time Priority**: Orders matched based on best price first, then by time of arrival
- **Advanced Order Conditions**:
  - **FOK (Fill Or Kill)**: Order must be completely filled immediately or cancelled
  - **IOC (Immediate Or Cancel)**: Fill as much as possible immediately, cancel the rest
  - **BOC (Book Or Cancel)**: Order must not match immediately or it's cancelled
- **Fee Calculation**: Separate maker and taker fee support
- **Market Depth Tracking**: Real-time order book depth monitoring
- **Self-Match Prevention**: Configurable actions to prevent self-trading
- **Efficient Data Structures**: Custom indexed heap implementation for optimal performance

## 📁 Project Structure

```
MatchingEngine/
├── DataStructures/
│   ├── IndexedHeap.cs       # Optimized heap with O(log n) insert/remove/find
│   └── OrderBook.cs         # Order book management with bid/ask levels
├── Enum/
│   ├── CancelReason.cs      # Reasons for order cancellation
│   ├── DataAction.cs        # CRUD operation types
│   ├── MatchState.cs        # Matching operation result states
│   ├── MatchStatus.cs       # Match status (deprecated)
│   ├── OrderCondition.cs    # Order conditions (FOK, IOC, BOC)
│   ├── OrderStatus.cs       # Order lifecycle statuses
│   ├── QueryAction.cs       # Query operation types
│   ├── SelfMatchAction.cs   # Actions for self-match scenarios
│   ├── TradingType.cs       # Trading match types
│   └── TransactionType.cs   # Transaction types
├── Models/
│   ├── BookInfo.cs          # Order book depth information
│   ├── Depth.cs             # Price level depth data
│   ├── MarketInfo.cs        # Market statistics (OHLC, volume, etc.)
│   ├── Matching.cs          # Match result details
│   ├── Ordering.cs          # Order entity with all properties
│   └── Transaction.cs       # Transaction details
└── MatchingEngine.cs        # Core matching engine logic
```

## 🏗️ Architecture

### Core Components

#### 1. MatchingEngine (`OptimizeMatchingEngine`)
The main engine that orchestrates order matching operations:
- Accepts and validates incoming orders
- Executes order matching logic
- Handles order cancellations
- Calculates fees for matched trades
- Validates order conditions (FOK, IOC, BOC)

**Key Methods:**
- `AddOrder(Ordering order, long timestamp, bool isOrderTriggered)` - Add a new order to the book
- `CancelOrder(Guid id)` - Cancel an existing order
- `TryMatchOrder(Ordering incomingOrder)` - Attempt to match an order against the book

#### 2. OrderBook
Manages the order book with bid and ask sides:
- Uses **indexed heaps** for O(log n) operations
- Maintains price levels with linked lists of orders (price-time priority)
- Provides fast access to best bid/ask prices
- Supports efficient order insertion, removal, and matching

**Key Features:**
- Max heap for bids (highest price first)
- Min heap for asks (lowest price first)
- Dictionary-based price level tracking
- Cached sorted prices for efficient iteration
- O(1) best price lookup
- O(log n) order insertion/removal

#### 3. IndexedHeap
A custom heap implementation that supports:
- O(1) peek operation
- O(log n) insert operation
- O(log n) extract operation
- O(log n) remove by value operation (via index map)
- Configurable as min-heap or max-heap

### Data Flow

```
Order Submission
      ↓
   Validation
      ↓
Order Conditions Check (FOK/IOC/BOC)
      ↓
  Add to OrderBook
      ↓
  Try to Match
      ↓
┌─────┴─────┐
│           │
Match Found  No Match
│           │
Execute      Keep in Book
│
Fee Calculation
│
Notify Handler
```

## 📊 Order Lifecycle

```
Prepared → Listed → [Matched] → Filled/Cancelled/Rejected
                         ↓
                    Reduced (partial fill)
```

### Order Statuses

- **Undefined** - Initial state
- **Prepared** - Order created but not yet submitted
- **Listed** - Order accepted and added to the book
- **Matched** - Order partially or fully matched
- **Filled** - Order completely filled
- **Cancelled** - Order cancelled by user or system
- **Rejected** - Order rejected due to invalid conditions
- **Reduced** - Order partially filled and reduced
- **Triggered** - Stop order triggered
- **Expired** - Order expired (time-based)

## 🔧 Order Conditions

### FOK (Fill Or Kill)
- Order must be **completely filled immediately**
- If not enough liquidity, the entire order is **cancelled**
- Cannot be a stop order
- Use case: Large orders that need full execution or none

### IOC (Immediate Or Cancel)
- Fill as much as possible **immediately**
- Any unfilled portion is **cancelled**
- Allows partial fills
- Use case: Orders that need immediate execution with no book presence

### BOC (Book Or Cancel)
- Order must **NOT match immediately**
- If it would match, the order is **cancelled**
- Ensures order is added to the book as a maker
- Use case: Market makers who want to provide liquidity only

## 💡 Usage Example

```csharp
// Initialize the matching engine
var engine = new OptimizeMatchingEngine(
    stepSize: 0.01m,          // Minimum price increment
    pricePrecision: 2,        // Decimal places for prices
    makerFeeRate: 0.001m,     // 0.1% maker fee
    takerFeeRate: 0.002m      // 0.2% taker fee
);

// Create and submit a buy order
var buyOrder = new Ordering
{
    OrdId = Guid.NewGuid(),
    IsBuy = true,
    Price = 100.50m,
    Volume = 10m,
    User = "user123",
    Condition = OrderCondition.None,
    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
};

// Add order to the matching engine
var result = engine.AddOrder(buyOrder, buyOrder.Timestamp);

// Handle the result
switch (result)
{
    case MatchState.OrderAccepted:
        Console.WriteLine("Order accepted and processed");
        break;
    case MatchState.FOKCannotFill:
        Console.WriteLine("Fill-or-kill order could not be filled");
        break;
    case MatchState.BOCCannotBook:
        Console.WriteLine("Book-or-cancel order would match immediately");
        break;
    default:
        Console.WriteLine($"Order result: {result}");
        break;
}

// Cancel an order
var cancelResult = engine.CancelOrder(buyOrder.OrdId);

// Access order book information
var bestBid = engine.Books.BestBidPrice;
var bestAsk = engine.Books.BestAskPrice;
Console.WriteLine($"Spread: {bestBid} - {bestAsk}");
```

## 🎯 Matching Algorithm

The engine uses **price-time priority** matching:

1. **Best Price First**: Orders with better prices are matched first
   - For buy orders: highest bid price
   - For sell orders: lowest ask price

2. **Time Priority**: Among orders at the same price, earlier orders are matched first (FIFO)

3. **Matching Process**:
   ```
   1. New order arrives
   2. Check if it can match with best opposite side order
   3. If prices cross:
      - Calculate match volume (minimum of both orders)
      - Execute match at resting order's price
      - Calculate fees for both sides
      - Update order volumes
      - Remove filled orders from book
      - Notify handlers
   4. Repeat until no more matches possible
   5. If order has remaining volume, add to book
   ```

## 📈 Performance Characteristics

| Operation | Time Complexity | Description |
|-----------|----------------|-------------|
| Add Order | O(log n) | Insert into heap and price level |
| Cancel Order | O(log n) | Remove from heap and price level |
| Get Best Price | O(1) | Peek heap top |
| Match Order | O(k log n) | k matches, each O(log n) |
| Find Order | O(1) | Dictionary lookup |

Where:
- `n` = number of price levels
- `k` = number of matches for an order

### 🚀 Real-World Performance

**Tested with 10 million orders:**
- **Throughput**: **433,303 orders/second** 🔥
- **Latency**: ~2.3 microseconds per order
- **Match Rate**: 45% (4.5M orders matched)
- **Order Book**: 40 price levels for 5.4M orders
- **Memory**: ~100-150 bytes per order

See [PERFORMANCE.md](PERFORMANCE.md) for detailed analysis and benchmarks.

## 💰 Fee Structure

The matching engine uses a simple maker-taker fee model:

- **Maker Fee**: Applied to orders that add liquidity to the book (resting orders)
- **Taker Fee**: Applied to orders that remove liquidity from the book (incoming orders that match)

Fees are configurable at engine initialization:
```csharp
var engine = new OptimizeMatchingEngine(
    stepSize: 0.01m,
    pricePrecision: 2,
    makerFeeRate: 0.001m,  // 0.1% for makers
    takerFeeRate: 0.002m   // 0.2% for takers
);
```

Fee calculation:
- **For buyers**: `fee = volume × feeRate`
- **For sellers**: `fee = volume × price × feeRate`

## 🎲 Self-Match Actions

When an order from the same user matches with their existing order:

- **Match** - Allow the self-match (default)
- **Reduce** - Decrease the volume of the existing order
- **Reject** - Cancel the new order if existing order found
- **Replace** - Cancel the existing order

## 📊 Market Information

The engine tracks comprehensive market statistics:
- **Open/Close Prices** - Session prices
- **High/Low Prices** - Price range
- **Total Volume** - Cumulative traded volume
- **Current Price** - Last trade price
- **Price Change Percentage** - Session change

## 🛠️ Technical Details

### Technologies
- **.NET 8.0** - Target framework
- **C# 12** - Language version
- **Nullable Reference Types** - Enabled for null safety

### Key Design Patterns
- **Repository Pattern** - Order storage and retrieval via OrderBook
- **Factory Pattern** - Order and match creation
- **Template Method** - Order matching flow with extensible conditions

### Memory Efficiency
- Price level pooling via dictionaries
- Order indexing for O(1) lookups
- Cached sorted price lists
- Linked lists for FIFO order queues at each price level

## 🚦 Match States

| State | Code | Description |
|-------|------|-------------|
| OrderAccepted | 1 | Order successfully accepted |
| CancelAccepted | 2 | Cancellation successful |
| OrderValid | 3 | Order validated |
| OrderNotExists | 11 | Order ID not found |
| OrderInvalid | 12 | Invalid order parameters |
| BOCCannotBook | 31 | BOC order would match immediately |
| FOKCannotFill | 32 | FOK order cannot be completely filled |
| IOCCannotFill | 33 | IOC order cannot be filled |
| SystemError | 99 | Internal system error |

## 🧪 Testing Recommendations

1. **Unit Tests**: Test individual components (OrderBook, IndexedHeap, MatchingEngine)
2. **Integration Tests**: Test complete order flows
3. **Performance Tests**: Load testing with thousands of orders
4. **Edge Cases**:
   - Empty order book
   - Self-matching scenarios
   - FOK/IOC/BOC conditions
   - Partial fills
   - Concurrent order submissions

## 📝 License

This project structure suggests it's a proprietary trading engine implementation.

## 🤝 Contributing

When contributing to this engine, please ensure:
1. Order matching remains deterministic
2. Price-time priority is maintained
3. All order conditions are respected
4. Performance characteristics are preserved
5. Thread safety is considered for concurrent operations

## ⚠️ Important Notes

1. **Thread Safety**: The current implementation is **not thread-safe**. For production use, add proper locking or use concurrent data structures.

2. **Persistence**: Orders are stored in-memory only. Implement database persistence for production systems.

3. **Matching Price**: Uses the **resting order's price** (maker price) for matches, which is standard in most exchanges.

4. **Fee Calculation**:
   - Buyer (taker): Fee on volume
   - Seller (maker): Fee on total cost (volume × price)

5. **Stop Orders**: Framework supports stop orders but implementation requires additional logic.

6. **Event Handling**: The engine processes orders synchronously. For asynchronous event handling (notifications, logging, etc.), consider wrapping the engine with an event publisher.

## 🔮 Future Enhancements

- [ ] Event-driven architecture with callbacks/handlers for match events
- [ ] Thread-safe concurrent operations
- [ ] Database persistence layer
- [ ] Stop-loss and take-profit orders
- [ ] Iceberg order support
- [ ] Order book snapshots
- [ ] Performance metrics and monitoring
- [ ] Market order support
- [ ] Good-Till-Date (GTD) order validity
- [ ] WebSocket API for real-time updates
- [ ] Historical trade data
- [ ] Circuit breakers and rate limiting

## 📚 Additional Resources

- **Order Book Mechanics**: Understanding limit order books
- **Market Microstructure**: How exchanges work
- **Price-Time Priority**: Standard matching algorithm
- **Order Types**: FOK, IOC, BOC, and other conditions

---

**Built for high-frequency trading environments where performance and reliability are critical.**
