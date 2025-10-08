using System.Diagnostics;
using Enum;
using MatchEngine.Models;

namespace MatchingEngine;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║          MATCHING ENGINE - ORDER TEST                     ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");

        // Get number of orders from user
        Console.Write("Enter number of orders to generate (default 100000): ");
        var input = Console.ReadLine();
        var orderCount = string.IsNullOrWhiteSpace(input) ? 100000 : int.Parse(input);

        Console.WriteLine($"\n═══════════════════════════════════════════════════════════");
        Console.WriteLine($"  Step 1: Generating {orderCount:N0} orders...");
        Console.WriteLine($"  Distribution: Limit (60%), Market (20%), IOC (10%), BOC (5%), FOK (5%)");
        Console.WriteLine($"═══════════════════════════════════════════════════════════\n");

        // Generate orders (NOT timed)
        var genStopwatch = Stopwatch.StartNew();
        var orders = GenerateRandomOrders(orderCount, "BTCUSDT");
        genStopwatch.Stop();
        
        Console.WriteLine($"  Orders generated in {genStopwatch.ElapsedMilliseconds:N0}ms");
        Console.WriteLine($"  Orders stored in memory: {orders.Count:N0}\n");

        Console.WriteLine($"═══════════════════════════════════════════════════════════");
        Console.WriteLine($"  Step 2: Initializing matching engine...");
        Console.WriteLine($"═══════════════════════════════════════════════════════════\n");

        // Initialize engine
        var engine = new MatchingEngine(
            stepSize: 0.01m,
            pricePrecision: 2,
            makerFeeRate: 0.001m,
            takerFeeRate: 0.002m
        );

        Console.WriteLine($"  Engine ready!\n");

        Console.WriteLine($"═══════════════════════════════════════════════════════════");
        Console.WriteLine($"  Step 3: Processing {orders.Count:N0} orders...");
        Console.WriteLine($"═══════════════════════════════════════════════════════════\n");

        // NOW start timing (only for processing)
        var stopwatch = Stopwatch.StartNew();

        int processedOrders = 0;
        int acceptedOrders = 0;
        int rejectedOrders = 0;
        int matchedOrders = 0;
        int bocRejected = 0;
        int fokRejected = 0;

        foreach (var order in orders)
        {
            var result = engine.AddOrder(order, order.Timestamp);
            processedOrders++;

            if (result == MatchState.OrderAccepted)
            {
                acceptedOrders++;
                if (order.IsFilled)
                    matchedOrders++;
            }
            else if (result == MatchState.BOCCannotBook)
            {
                bocRejected++;
                rejectedOrders++;
            }
            else if (result == MatchState.FOKCannotFill)
            {
                fokRejected++;
                rejectedOrders++;
            }
            else
            {
                rejectedOrders++;
            }
        }

        stopwatch.Stop();
        var tps = processedOrders / stopwatch.Elapsed.TotalSeconds;

        // Print results
        Console.WriteLine($"\n╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine($"║                    RESULTS                                ║");
        Console.WriteLine($"╚═══════════════════════════════════════════════════════════╝\n");

        Console.WriteLine($"  ┌─────────────────────────────────────────────────────────┐");
        Console.WriteLine($"  │ ORDER STATISTICS                                        │");
        Console.WriteLine($"  └─────────────────────────────────────────────────────────┘");
        Console.WriteLine($"  Orders Processed:    {processedOrders:N0}");
        Console.WriteLine($"  Accepted:            {acceptedOrders:N0} ({acceptedOrders * 100.0 / processedOrders:F1}%)");
        Console.WriteLine($"  Rejected:            {rejectedOrders:N0} ({rejectedOrders * 100.0 / processedOrders:F1}%)");
        Console.WriteLine($"    - BOC Rejected:    {bocRejected:N0}");
        Console.WriteLine($"    - FOK Rejected:    {fokRejected:N0}");
        Console.WriteLine($"  Fully Matched:       {matchedOrders:N0}");
        Console.WriteLine($"");
        Console.WriteLine($"  ┌─────────────────────────────────────────────────────────┐");
        Console.WriteLine($"  │ PERFORMANCE METRICS (Processing Only)                   │");
        Console.WriteLine($"  └─────────────────────────────────────────────────────────┘");
        Console.WriteLine($"  Processing Time:     {stopwatch.ElapsedMilliseconds:N0}ms ({stopwatch.Elapsed.TotalSeconds:F2}s)");
        Console.WriteLine($"  Throughput (TPS):    {tps:N0} orders/second");
        Console.WriteLine($"  Avg Latency:         {(stopwatch.ElapsedMilliseconds * 1000.0 / processedOrders):F3} μs/order");
        Console.WriteLine($"");
        Console.WriteLine($"  ┌─────────────────────────────────────────────────────────┐");
        Console.WriteLine($"  │ ORDER BOOK STATE                                        │");
        Console.WriteLine($"  └─────────────────────────────────────────────────────────┘");
        Console.WriteLine($"  Bid Levels:          {engine.Books.BidLevelCount}");
        Console.WriteLine($"  Ask Levels:          {engine.Books.AskLevelCount}");
        Console.WriteLine($"  Total Price Levels:  {engine.Books.BidLevelCount + engine.Books.AskLevelCount}");
        Console.WriteLine($"  Orders in Book:      {engine.Books.TotalOrders:N0}");
        Console.WriteLine($"  Best Bid:            ${engine.Books.BestBidPrice:N2}");
        Console.WriteLine($"  Best Ask:            ${engine.Books.BestAskPrice:N2}");
        Console.WriteLine($"  Spread:              ${(engine.Books.BestAskPrice - engine.Books.BestBidPrice):N2}");

        var memoryMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
        Console.WriteLine($"");
        Console.WriteLine($"  ┌─────────────────────────────────────────────────────────┐");
        Console.WriteLine($"  │ MEMORY USAGE                                            │");
        Console.WriteLine($"  └─────────────────────────────────────────────────────────┘");
        Console.WriteLine($"  Total Memory:        {memoryMB:F2} MB");
        Console.WriteLine($"  Per Order in Book:   {(memoryMB * 1024 * 1024 / engine.Books.TotalOrders):F0} bytes");
        Console.WriteLine($"");
        Console.WriteLine($"  ┌─────────────────────────────────────────────────────────┐");
        Console.WriteLine($"  │ TIMING BREAKDOWN                                        │");
        Console.WriteLine($"  └─────────────────────────────────────────────────────────┘");
        Console.WriteLine($"  Generation Time:     {genStopwatch.ElapsedMilliseconds:N0}ms (not counted in TPS)");
        Console.WriteLine($"  Processing Time:     {stopwatch.ElapsedMilliseconds:N0}ms (pure engine performance)");
        Console.WriteLine($"  Total Time:          {(genStopwatch.ElapsedMilliseconds + stopwatch.ElapsedMilliseconds):N0}ms");

        Console.WriteLine($"\n╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine($"║                  COMPLETE! ✅                             ║");
        Console.WriteLine($"╚═══════════════════════════════════════════════════════════╝");

        Console.ReadLine();
    }

    static List<Ordering> GenerateRandomOrders(int count, string symbol)
    {
        var orders = new List<Ordering>(count); // Pre-allocate capacity
        var random = new Random(42);
        var basePrice = 50000m;
        var baseTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // Generate ONCE

        // Counters for statistics
        int limitCount = 0, marketCount = 0, iocCount = 0, bocCount = 0, fokCount = 0;

        for (int i = 0; i < count; i++)
        {
            var isBuy = random.Next(2) == 0; // 50/50 buy/sell
            var rand = random.NextDouble();

            decimal price;
            OrderCondition condition;
            decimal volume = Math.Round((decimal)(random.NextDouble() * 10 + 0.1), 4);

            // Distribution: 60% Limit, 20% Market, 10% IOC, 5% BOC, 5% FOK
            if (rand < 0.60) // 60% LIMIT ORDERS
            {
                var priceOffset = (decimal)(random.NextDouble() * 1000 - 500);
                price = Math.Round(basePrice + priceOffset, 2);
                condition = OrderCondition.None;
                limitCount++;
            }
            else if (rand < 0.80) // 20% MARKET ORDERS
            {
                price = 0;
                condition = OrderCondition.IOC;
                marketCount++;
            }
            else if (rand < 0.90) // 10% IOC ORDERS
            {
                var priceOffset = (decimal)(random.NextDouble() * 1000 - 500);
                price = Math.Round(basePrice + priceOffset, 2);
                condition = OrderCondition.IOC;
                iocCount++;
            }
            else if (rand < 0.95) // 5% BOC ORDERS
            {
                var priceOffset = isBuy
                    ? -(decimal)(random.NextDouble() * 300 + 200)
                    : (decimal)(random.NextDouble() * 300 + 200);
                price = Math.Round(basePrice + priceOffset, 2);
                condition = OrderCondition.BOC;
                bocCount++;
            }
            else // 5% FOK ORDERS
            {
                var priceOffset = (decimal)(random.NextDouble() * 1000 - 500);
                price = Math.Round(basePrice + priceOffset, 2);
                condition = OrderCondition.FOK;
                volume = Math.Round((decimal)(random.NextDouble() * 5 + 0.5), 4);
                fokCount++;
            }

            orders.Add(new Ordering
            {
                OrdId = Guid.NewGuid(),
                OrdNo = i + 1,
                Symbol = symbol,
                User = $"user{random.Next(1, 100)}",
                IsBuy = isBuy,
                Price = price,
                Volume = volume,
                Condition = condition,
                Status = OrderStatus.Prepared,
                Timestamp = baseTimestamp + i  // Increment from base (no system call!)
            });
        }

        // Print distribution
        Console.WriteLine($"  Order Types Generated:");
        Console.WriteLine($"    Limit (None):      {limitCount:N0} ({limitCount * 100.0 / count:F1}%)");
        Console.WriteLine($"    Market:            {marketCount:N0} ({marketCount * 100.0 / count:F1}%)");
        Console.WriteLine($"    IOC:               {iocCount:N0} ({iocCount * 100.0 / count:F1}%)");
        Console.WriteLine($"    BOC:               {bocCount:N0} ({bocCount * 100.0 / count:F1}%)");
        Console.WriteLine($"    FOK:               {fokCount:N0} ({fokCount * 100.0 / count:F1}%)");
        Console.WriteLine();

        return orders;
    }
}
