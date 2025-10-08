# Matching Engine - Usage Guide

## 🚀 Quick Start

### Run the Test

```bash
dotnet run --configuration Release
```

### Input

```
Enter number of orders to generate (default 100000): 1000000
```

- Press **Enter** for default (100,000 orders)
- Or enter any number you want to test

## 📊 What Happens

### Step 1: Generate Orders (Not Timed)
The program generates random orders with realistic distribution:

```
Order Types Distribution:
- 60% Limit Orders (None)     - Normal limit orders that go into the book
- 20% Market Orders (IOC)      - Market orders that execute immediately
- 10% IOC Orders               - Immediate-Or-Cancel with price limit
- 5% BOC Orders                - Book-Or-Cancel (maker only)
- 5% FOK Orders                - Fill-Or-Kill (all-or-nothing)
```

Orders are stored in memory before processing begins.

### Step 2: Initialize Engine
The matching engine is initialized with:
- Step size: 0.01
- Price precision: 2 decimals
- Maker fee: 0.1%
- Taker fee: 0.2%

### Step 3: Process Orders (Timed)
**This is where TPS measurement starts!**

The engine processes all orders from the pre-generated array:
- Matching logic
- Order book updates
- Fee calculations
- Condition validations (FOK, IOC, BOC)

**TPS calculation is based ONLY on processing time, not generation time.**

## 📈 Sample Output

```
╔═══════════════════════════════════════════════════════════╗
║          MATCHING ENGINE - ORDER TEST                     ║
╚═══════════════════════════════════════════════════════════╝

Enter number of orders to generate (default 100000): 1000000

═══════════════════════════════════════════════════════════
  Step 1: Generating 1,000,000 orders...
  Distribution: Limit (60%), Market (20%), IOC (10%), BOC (5%), FOK (5%)
═══════════════════════════════════════════════════════════

  Order Types Generated:
    Limit (None):      600,043 (60.0%)
    Market:            199,987 (20.0%)
    IOC:               100,012 (10.0%)
    BOC:                49,991 (5.0%)
    FOK:                49,967 (5.0%)

  Orders generated in 125ms
  Orders stored in memory: 1,000,000

═══════════════════════════════════════════════════════════
  Step 2: Initializing matching engine...
═══════════════════════════════════════════════════════════

  Engine ready!

═══════════════════════════════════════════════════════════
  Step 3: Processing 1,000,000 orders...
═══════════════════════════════════════════════════════════

╔═══════════════════════════════════════════════════════════╗
║                    RESULTS                                ║
╚═══════════════════════════════════════════════════════════╝

  ┌─────────────────────────────────────────────────────────┐
  │ ORDER STATISTICS                                        │
  └─────────────────────────────────────────────────────────┘
  Orders Processed:    1,000,000
  Accepted:            925,432 (92.5%)
  Rejected:            74,568 (7.5%)
    - BOC Rejected:    38,234
    - FOK Rejected:    36,334
  Fully Matched:       623,456

  ┌─────────────────────────────────────────────────────────┐
  │ PERFORMANCE METRICS (Processing Only)                   │
  └─────────────────────────────────────────────────────────┘
  Processing Time:     2,310ms (2.31s)
  Throughput (TPS):    433,045 orders/second
  Avg Latency:         2.310 μs/order

  ┌─────────────────────────────────────────────────────────┐
  │ ORDER BOOK STATE                                        │
  └─────────────────────────────────────────────────────────┘
  Bid Levels:          27
  Ask Levels:          13
  Total Price Levels:  40
  Orders in Book:      301,976
  Best Bid:            $49,998.50
  Best Ask:            $50,001.25
  Spread:              $2.75

  ┌─────────────────────────────────────────────────────────┐
  │ MEMORY USAGE                                            │
  └─────────────────────────────────────────────────────────┘
  Total Memory:        425.67 MB
  Per Order in Book:   1,410 bytes

  ┌─────────────────────────────────────────────────────────┐
  │ TIMING BREAKDOWN                                        │
  └─────────────────────────────────────────────────────────┘
  Generation Time:     125ms (not counted in TPS)
  Processing Time:     2,310ms (pure engine performance)
  Total Time:          2,435ms

╔═══════════════════════════════════════════════════════════╗
║                  COMPLETE! ✅                             ║
╚═══════════════════════════════════════════════════════════╝
```

## 🎯 Understanding the Metrics

### TPS (Transactions Per Second)
**Only measures order processing performance**, not generation time.

Formula: `TPS = Total Orders / Processing Time (seconds)`

Example:
- 1,000,000 orders processed in 2.31 seconds
- TPS = 1,000,000 / 2.31 = **433,045 orders/second**

### Average Latency
Time to process one order on average.

Formula: `Latency = (Processing Time × 1000) / Total Orders` (in microseconds)

Example:
- 2,310ms for 1,000,000 orders
- Latency = (2,310 × 1000) / 1,000,000 = **2.310 μs/order**

### Order Book State
- **Bid/Ask Levels**: Number of unique price points
- **Total Price Levels**: Sum of bid and ask levels
- **Orders in Book**: Orders waiting to be matched
- **Spread**: Price difference between best bid and best ask

### Memory Usage
- **Total Memory**: Memory used by the process
- **Per Order**: Average memory per order in the book

## 🔧 Test Sizes

| Size | Orders | Use Case |
|------|--------|----------|
| **Small** | 10K - 100K | Quick smoke test |
| **Medium** | 100K - 1M | Standard benchmark |
| **Large** | 1M - 5M | Stress test |
| **X-Large** | 5M - 10M+ | Maximum capacity test |

## 💡 Tips

### For Accurate TPS
- Use **Release** build: `dotnet run --configuration Release`
- Close other applications to reduce CPU/memory interference
- Run multiple times and take average
- Use consistent test sizes for comparison

### For Memory Testing
- Test with large order counts (5M+)
- Monitor system memory availability
- Check per-order memory usage

### For Latency Testing
- Smaller test sizes (100K-1M) for stable measurements
- Release build is critical
- Multiple runs for statistical significance

## 🎲 Order Generation Details

### Random Distribution
- **50/50** buy/sell split
- **Price**: Base $50,000 ± $500 (random spread)
- **Volume**: 0.1 - 10 BTC (random)

### Order Types
1. **Limit (60%)**: Price = base ± $500, Condition = None
2. **Market (20%)**: Price = 0, Condition = IOC
3. **IOC (10%)**: Price = base ± $500, Condition = IOC
4. **BOC (5%)**: Price = base ± $200-500 (away from market), Condition = BOC
5. **FOK (5%)**: Price = base ± $500, Volume = 0.5-5 BTC, Condition = FOK

### Why This Distribution?
This mimics real-world trading patterns:
- Most orders are limit orders (provide liquidity)
- Market orders for immediate execution
- IOC for aggressive takers
- BOC for market makers
- FOK for institutional orders

## 📝 Notes

### TPS Measurement
- **Separated** generation time from processing time
- TPS reflects **pure matching engine performance**
- No I/O, no network, no database - pure in-memory processing

### Limitations
- Single-threaded (no concurrency)
- In-memory only (no persistence)
- No order validation beyond matching logic
- No external fee lookups

### Future Enhancements
- Multi-threaded processing
- Database persistence
- WebSocket API
- Real-time order book updates
- Advanced order types

---

**Built for performance testing and benchmarking.** 🚀
