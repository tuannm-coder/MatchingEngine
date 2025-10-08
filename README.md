# Matching Engine - Hệ Thống Khớp Lệnh

Matching engine cho sàn giao dịch cryptocurrency/chứng khoán, implement thuật toán **Price-Time Priority** (giá tốt nhất trước, cùng giá thì ai đến trước được ưu tiên).

## 🏗️ Kiến Trúc Tổng Quan

```
User Order
    ↓
MatchingEngine
    ↓
OrderBook
    ├─ Heap (Best Price)
    ├─ PriceLevel (Volume per Price)
    └─ LinkedList (FIFO Orders)
```

## 📊 Cấu Trúc Dữ Liệu

### 1. MatchingEngine

**Trách nhiệm:** Điều phối matching logic, validate conditions

**Lưu trữ:**
```csharp
private readonly int _precision;           // Số chữ số thập phân (2 = $50.12)
private readonly decimal _stepSize;        // Bước giá tối thiểu (0.01 = $0.01)
private readonly decimal _makerFeeRate;    // Phí maker (0.001 = 0.1%)
private readonly decimal _takerFeeRate;    // Phí taker (0.002 = 0.2%)
public OrderBook Books;                    // Sổ lệnh chính
```

**KHÔNG lưu orders** - tất cả orders nằm trong OrderBook!

---

### 2. OrderBook

**Trách nhiệm:** Quản lý tất cả orders, tìm best price, matching

**Lưu trữ (3 layers):**

#### Layer 1: Heap - Tìm Best Price O(1)
```csharp
private IndexedHeap<decimal> _bidHeap;   // Max heap - giá cao nhất
private IndexedHeap<decimal> _askHeap;   // Min heap - giá thấp nhất
```

**Lưu gì:** CHỈ lưu **giá** (decimal), KHÔNG lưu volume hay orders

**Ví dụ:**
```
_bidHeap (Max Heap):
  Root: 50200 ← Best bid
  Children: [50100, 50150, 49900, 50000]

_askHeap (Min Heap):
  Root: 50300 ← Best ask
  Children: [50400, 50350, 50500, 50600]
```

**Tại sao:** Peek O(1), Insert/Remove O(log n)

---

#### Layer 2: Dictionary - Map Price → PriceLevel
```csharp
private Dictionary<decimal, BidPriceLevel> _bidLevels;
private Dictionary<decimal, AskPriceLevel> _askLevels;
```

**Lưu gì:** Map từ **giá → PriceLevel** object

**Ví dụ:**
```
_bidLevels:
  50200 → BidPriceLevel { TotalVolume: 15.5 BTC, Orders: [...] }
  50100 → BidPriceLevel { TotalVolume: 8.3 BTC, Orders: [...] }
  50000 → BidPriceLevel { TotalVolume: 12.1 BTC, Orders: [...] }

_askLevels:
  50300 → AskPriceLevel { TotalVolume: 10.2 BTC, Orders: [...] }
  50400 → AskPriceLevel { TotalVolume: 6.7 BTC, Orders: [...] }
```

**Tại sao:** Lookup O(1) theo giá

---

#### Layer 3: PriceLevel - Aggregate Volume + FIFO Queue
```csharp
public class PriceLevel
{
    public decimal Price;                        // Giá của level này
    public decimal TotalVolume;                  // Tổng volume tất cả orders
    public LinkedList<Ordering> Orders;          // FIFO queue các orders
    public DateTime LastUpdated;
}
```

**Lưu gì:**
- **TotalVolume**: Tổng volume của tất cả orders ở giá này
- **Orders**: LinkedList chứa full chi tiết từng order (FIFO)

**Ví dụ:**
```
PriceLevel @ $50,000:
  TotalVolume: 10.5 BTC
  Orders: LinkedList
    ├─ Order 1: Alice, 1.0 BTC (đến trước)
    ├─ Order 2: Bob, 2.0 BTC
    ├─ Order 3: Charlie, 1.5 BTC
    └─ Order 4: Dave, 6.0 BTC (đến sau)
```

**Tại sao LinkedList:** 
- FIFO (First In First Out)
- Add/Remove O(1)
- Time priority tự động

---

#### Bonus: Order Index - Tìm Order Theo ID
```csharp
private Dictionary<Guid, OrderNode> _orderIndex;
```

**Lưu gì:** Map từ **OrderID → OrderNode**

**Ví dụ:**
```
_orderIndex:
  "abc-123" → OrderNode { Order: {...}, ListNode: pointer to LinkedList node }
  "def-456" → OrderNode { Order: {...}, ListNode: pointer to LinkedList node }
```

**Tại sao:** 
- Tìm order theo ID O(1)
- Cancel order nhanh
- Không cần duyệt toàn bộ book

---

#### Cache (Tối ưu cho FOK)
```csharp
private List<decimal>? _sortedBidPrices;    // Cache sorted prices
private List<decimal>? _sortedAskPrices;
private bool _bidCacheDirty;                // Dirty flag
private bool _askCacheDirty;
```

**Lưu gì:** Danh sách giá đã sort

**Khi nào dùng:** CHỈ cho FOK orders (Fill-Or-Kill)

**Tại sao cần:**
- FOK phải check toàn bộ liquidity
- Reuse cache thay vì sort lại mỗi lần
- Nhanh hơn 192 lần!

---

## 🔄 Luồng Dữ Liệu

### A. Add Order (Thêm Lệnh Mới)

```
1. User tạo Order
   ↓
2. MatchingEngine.AddOrder(order)
   ├─ Validate order (null check)
   ├─ Check market order (price = 0?)
   ├─ CheckOrderConditions (FOK/BOC/IOC)
   ├─ For IOC/Market: TryMatch ngay
   └─ For Limit: Add to book → TryMatch
   ↓
3. OrderBook.AddOrder(order)
   ├─ Create OrderNode
   ├─ Add to _orderIndex (by ID)
   └─ Add to PriceLevel
       ├─ Check price level exists?
       ├─ NO → Create new level
       │   ├─ Add to _bidLevels / _askLevels
       │   ├─ Insert price to heap
       │   └─ InvalidateCache() ← Mark dirty
       └─ YES → Use existing level
           └─ Add order to LinkedList (FIFO)
   ↓
4. PriceLevel.AddOrder(order)
   ├─ Orders.AddLast(order) ← Vào cuối queue
   └─ TotalVolume += order.Volume
```

### B. Match Order (Khớp Lệnh)

```
1. MatchingEngine.TryMatchOrder(incomingOrder)
   ↓
2. Loop while order chưa filled:
   ├─ GetBestBidOrder() / GetBestAskOrder()
   │   ├─ Get best price from heap O(1)
   │   └─ Get first order in LinkedList O(1)
   ├─ Check giá có cross không?
   └─ ExecuteMatch(incoming, resting)
       ↓
3. ExecuteMatch()
   ├─ matchPrice = resting.Price
   ├─ matchVolume = Min(incoming.Volume, resting.Volume)
   ├─ Update volumes:
   │   ├─ incoming.Volume -= matchVolume
   │   └─ resting.Volume -= matchVolume
   ├─ Calculate fees (maker/taker)
   ├─ Update costs
   └─ If resting filled:
       └─ OrderBook.FillOrder(resting)
           ├─ PriceLevel.TotalVolume -= matchVolume
           └─ RemoveOrder() ← Cleanup
               ├─ Remove from _orderIndex
               ├─ Remove from LinkedList
               └─ If level empty:
                   ├─ Remove from heap
                   ├─ Remove from dictionary
                   └─ InvalidateCache()
```

### C. FOK Check (Fill-Or-Kill)

```
1. Order.Condition == FOK
   ↓
2. OrderBook.CheckCanFillOrder(isBuy, volume, limitPrice)
   ├─ GetSortedAskPrices() / GetSortedBidPrices()
   │   ├─ Check cache dirty?
   │   ├─ YES → Rebuild cache ← O(n log n) ~10 lần/test
   │   └─ NO → Reuse cache ← O(1) ~49,990 lần/test
   ├─ Foreach price in sorted order:
   │   └─ cumulativeVolume += PriceLevel.TotalVolume
   └─ Return: cumulativeVolume >= volume?
```

### D. Cancel Order (Hủy Lệnh)

```
1. MatchingEngine.CancelOrder(orderId)
   ↓
2. OrderBook.FindOrder(orderId)
   └─ _orderIndex.TryGetValue(orderId) ← O(1)
   ↓
3. OrderBook.RemoveOrder(orderId)
   ├─ Get PriceLevel from _bidLevels / _askLevels
   ├─ PriceLevel.RemoveOrder(order)
   │   ├─ Find in LinkedList
   │   ├─ Remove from LinkedList
   │   └─ TotalVolume -= order.Volume
   ├─ Remove from _orderIndex
   └─ If level empty:
       ├─ Remove level from dictionary
       ├─ Remove price from heap
       └─ InvalidateCache()
```

## 📦 Data Storage Example

### Ví Dụ: Order Book State

```
═══════════════════════════════════════════════════════════
BOOK STATE
═══════════════════════════════════════════════════════════

Heap (_bidHeap, _askHeap):
  _bidHeap.Peek() = 50200  ← Best bid O(1)
  _askHeap.Peek() = 50300  ← Best ask O(1)

Dictionaries (_bidLevels, _askLevels):
  50200 → BidPriceLevel
  50100 → BidPriceLevel
  50300 → AskPriceLevel
  50400 → AskPriceLevel

PriceLevel @ $50,200 (BID):
  TotalVolume: 4.5 BTC
  Orders: [Alice(1.0), Bob(2.0), Charlie(1.5)]
           ↑ First (match first)

PriceLevel @ $50,300 (ASK):
  TotalVolume: 3.2 BTC
  Orders: [Dave(1.2), Eve(2.0)]
           ↑ First (match first)

Order Index (_orderIndex):
  alice-id → OrderNode { Order: Alice's, ListNode: pointer }
  bob-id   → OrderNode { Order: Bob's, ListNode: pointer }
  ...

Cache (_sortedBidPrices, _sortedAskPrices):
  _sortedBidPrices: null (hoặc [50200, 50100] nếu có FOK)
  _bidCacheDirty: true (cần rebuild nếu access)
```

## 🎯 Tại Sao Thiết Kế Như Vậy?

### 3 Layers Architecture:

| Layer | Data Structure | Lưu Gì | Mục Đích | Complexity |
|-------|---------------|--------|----------|------------|
| **Heap** | Binary Heap | Prices only | Best price O(1) | Peek: O(1) |
| **Dictionary** | Hash Table | Price → PriceLevel | Price lookup O(1) | Get: O(1) |
| **PriceLevel** | LinkedList | Orders (FIFO) | Time priority O(1) | Add/Remove: O(1) |

### Separation of Concerns:

- **Heap**: Fast price comparison
- **Dictionary**: Fast price-to-level mapping  
- **PriceLevel**: FIFO queue + volume aggregation
- **OrderIndex**: Fast ID lookup

### Optimization Highlights:

✅ **Best Bid/Ask:** O(1) - peek heap
✅ **Find Order:** O(1) - dictionary lookup
✅ **Add Order:** O(log n) - heap insert
✅ **Match Order:** O(1) most cases - first in LinkedList
✅ **FOK Check:** O(n) with cache reuse - sorted iteration

## 🔢 Memory Layout

### Ví dụ: 10 triệu orders

```
Orders: 10,000,000 total
├─ Matched & removed: 4,500,000
└─ In book: 5,500,000

Price Levels: 40 total
├─ Bid levels: 27
└─ Ask levels: 13

Storage:
├─ Heap: 40 prices × 8 bytes = 320 bytes
├─ Dictionary: 40 entries × 24 bytes = 960 bytes
├─ PriceLevel: 40 × 32 bytes = 1,280 bytes
├─ Orders: 5.5M × 180 bytes = 990 MB
├─ OrderIndex: 5.5M × 24 bytes = 132 MB
└─ Cache: 40 prices × 8 bytes = 320 bytes (if built)
───────────────────────────────────────
Total: ~1,122 MB (~200 bytes/order)
```

## ⚡ Performance

**Test:** 1,000,000 orders (60% Limit, 20% Market, 10% IOC, 5% BOC, 5% FOK)

```
TPS: 400,000 orders/second
Latency: 2.5 microseconds/order
Match Rate: 45%
Price Levels: 40
Memory: ~200 bytes/order
```

**So sánh:**
- Binance: ~1M TPS (C++, multi-thread)
- **This engine: ~400K TPS** (C#, single-thread)
- Coinbase: ~200K TPS

## 🎯 Luồng Dữ Liệu Chi Tiết

### Scenario: Order Lifecycle

```
═══════════════════════════════════════════════════════════
PHASE 1: ORDER SUBMISSION
═══════════════════════════════════════════════════════════

Order {
  OrdId: abc-123
  IsBuy: true
  Price: 50000
  Volume: 10 BTC
  Condition: None
}
    ↓
MatchingEngine.AddOrder()
    ↓
Validate: ✅ OK
Market check: ✅ Not market (price > 0)
FOK check: ✅ Skip (not FOK)
BOC check: ✅ Skip (not BOC)

═══════════════════════════════════════════════════════════
PHASE 2: ADD TO BOOK
═══════════════════════════════════════════════════════════

OrderBook.AddOrder()
    ↓
[1] Create OrderNode
    OrderNode { Order: {...}, ListNode: null }
    
[2] Add to _orderIndex
    _orderIndex["abc-123"] = OrderNode
    
[3] Add to PriceLevel
    Check _bidLevels[50000] exists?
    
    NO → Create new:
      ├─ new BidPriceLevel(50000)
      ├─ _bidLevels[50000] = level
      ├─ _bidHeap.Insert(50000) ← Add price to heap
      └─ InvalidateBidCache() ← Mark cache dirty
    
    YES → Use existing level
    
[4] PriceLevel.AddOrder()
    ├─ LinkedList.AddLast(order) ← Vào cuối queue
    ├─ OrderNode.ListNode = node ← Save pointer
    └─ TotalVolume += 10 BTC

Book State Now:
  _bidHeap: [50200, 50100, 50000, ...] ← 50000 added
  _bidLevels[50000]: 
    TotalVolume: 10 BTC
    Orders: [Order abc-123]
  _orderIndex["abc-123"] → OrderNode

═══════════════════════════════════════════════════════════
PHASE 3: MATCHING
═══════════════════════════════════════════════════════════

MatchingEngine.TryMatchOrder(incomingOrder)
    ↓
[1] Get best opposite order
    OrderBook.GetBestAskOrder()
      ├─ bestPrice = _askHeap.Peek() ← O(1) get 50300
      └─ return _askLevels[50300].Orders.First ← O(1)
      
    restingOrder {
      OrdId: xyz-789
      IsBuy: false
      Price: 50300
      Volume: 3 BTC
    }

[2] Check can match?
    incoming.Price (50000) >= resting.Price (50300)?
    NO → STOP (giá không cross)

═══════════════════════════════════════════════════════════
RESULT: Order vào book, chờ match sau
═══════════════════════════════════════════════════════════
```

### Scenario: Matching Happens

```
New Order: SELL 15 BTC @ 49900

═══════════════════════════════════════════════════════════
MATCHING SEQUENCE
═══════════════════════════════════════════════════════════

[1] Get best bid: $50,200 (Alice 1.0 BTC)
    Can match? 49900 <= 50200 ✅ YES
    
    ExecuteMatch():
      matchPrice = 50200 (resting order's price)
      matchVolume = Min(15, 1.0) = 1.0 BTC
      
      incoming.Volume = 15 - 1 = 14 BTC
      resting.Volume = 1 - 1 = 0 BTC ← FILLED!
      
      FillOrder(resting):
        ├─ PriceLevel[50200].TotalVolume -= 1
        ├─ RemoveOrder(alice-id)
        │   ├─ Remove from _orderIndex
        │   └─ Remove from LinkedList
        └─ If level empty:
            ├─ Remove from _bidLevels
            ├─ Remove from _bidHeap
            └─ InvalidateCache()

[2] Get best bid: $50,100 (Bob 2.0 BTC)
    Can match? 49900 <= 50100 ✅ YES
    
    ExecuteMatch():
      matchVolume = Min(14, 2.0) = 2.0 BTC
      incoming.Volume = 14 - 2 = 12 BTC
      resting.Volume = 2 - 2 = 0 ← FILLED!
      FillOrder(resting) ...

[3] Continue until incoming filled or no more matches
    ...

═══════════════════════════════════════════════════════════
FINAL STATE
═══════════════════════════════════════════════════════════

Incoming order:
  - Matched: 15 BTC
  - Remaining: 0 BTC
  - Status: Filled
  - Avg Price: calculated from all matches

Order Book:
  - Best bid changed: 50200 → next level
  - Alice, Bob removed from book
  - Cache invalidated (nếu level bị xóa)
```

## 🔑 Key Concepts

### Price-Time Priority
1. **Price Priority:** Giá tốt hơn được ưu tiên
   - Bid: giá cao nhất trước (max heap)
   - Ask: giá thấp nhất trước (min heap)

2. **Time Priority:** Cùng giá → ai đến trước match trước
   - LinkedList = FIFO tự động
   - Không cần timestamp comparison

### Lazy Loading Cache
- **Không build** cho đến khi cần (FOK order)
- **Reuse** nhiều lần khi đã build
- **Invalidate** chỉ khi structure thay đổi
- **Rebuild** chỉ khi access và dirty

### Maker vs Taker
- **Maker:** Order vào book (provide liquidity) → fee thấp
- **Taker:** Order match ngay (take liquidity) → fee cao
- Resting order = maker, Incoming order = taker

## 📚 Order Types

| Type | Price | Condition | Behavior |
|------|-------|-----------|----------|
| **Limit** | > 0 | None | Vào book nếu không match |
| **Market** | = 0 | IOC | Match ngay, cancel dư |
| **IOC** | > 0 | IOC | Match ngay, cancel dư |
| **BOC** | > 0 | BOC | Chỉ vào book (không match) |
| **FOK** | > 0 | FOK | Fill toàn bộ hoặc reject |

## 🚀 Run Test

```bash
dotnet run --configuration Release

Enter number of orders: 1000000
Include FOK orders? (y/n): n

Result:
TPS: ~400,000 orders/second ✅
```

---

**Built with C# .NET 8.0 for high-performance order matching.**
