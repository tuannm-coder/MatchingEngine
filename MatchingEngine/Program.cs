using System.Diagnostics;
using Enum;
using MatchEngine.Models;
using MatchingEngine.Extension;

namespace MatchingEngine;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Matching Engine Performance Test ===\n");

        // Test với số lượng orders khác nhau
        var testCases = new[] { 10000000 };

        foreach (var orderCount in testCases)
        {
            Console.WriteLine($"\n--- Testing with {orderCount:N0} orders ---");
            
            //// Test Limit Orders
            //TestLimitOrders(orderCount);
            
            //// Test Market Orders
            //TestMarketOrders(orderCount);
            
            // Test Mixed Orders (Limit + Market)
            TestMixedOrders(orderCount);
        }

        Console.WriteLine("\n=== Test Complete ===");
        Console.ReadLine();
    }

    static void TestLimitOrders(int orderCount)
    {
        var engine = new MatchingEngine(
            stepSize: 0.01m,
            pricePrecision: 2,
            makerFeeRate: 0.001m,
            takerFeeRate: 0.002m
        );

        var orders = GenerateLimitOrders(orderCount, "BTCUSDT");
        var stopwatch = Stopwatch.StartNew();
        
        int processedOrders = 0;
        int matchedOrders = 0;

        foreach (var order in orders)
        {
            var result = engine.AddOrder(order, order.Timestamp);
            processedOrders++;
            
            if (result == MatchState.OrderAccepted)
            {
                if (order.IsFilled)
                    matchedOrders++;
            }
        }

        stopwatch.Stop();
        var tps = processedOrders / stopwatch.Elapsed.TotalSeconds;

        Console.WriteLine($"  [LIMIT ORDERS]");
        Console.WriteLine($"    Total Orders: {processedOrders:N0}");
        Console.WriteLine($"    Matched Orders: {matchedOrders:N0}");
        Console.WriteLine($"    Time: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"    TPS: {tps:N0} orders/second");
        Console.WriteLine($"    Order Book - Bid Levels: {engine.Books.BidLevelCount}, Ask Levels: {engine.Books.AskLevelCount}");
        Console.WriteLine($"    Best Bid: {engine.Books.BestBidPrice}, Best Ask: {engine.Books.BestAskPrice}");
    }

    static void TestMarketOrders(int orderCount)
    {
        var engine = new MatchingEngine(
            stepSize: 0.01m,
            pricePrecision: 2,
            makerFeeRate: 0.001m,
            takerFeeRate: 0.002m
        );

        // Tạo liquidity trước (limit orders)
        var liquidityOrders = GenerateLimitOrders(orderCount / 2, "BTCUSDT");
        foreach (var order in liquidityOrders)
        {
            engine.AddOrder(order, order.Timestamp);
        }

        // Tạo market orders
        var marketOrders = GenerateMarketOrders(orderCount / 2, "BTCUSDT");
        var stopwatch = Stopwatch.StartNew();
        
        int processedOrders = 0;
        int matchedOrders = 0;

        foreach (var order in marketOrders)
        {
            var result = engine.AddOrder(order, order.Timestamp);
            processedOrders++;
            
            if (result == MatchState.OrderAccepted)
            {
                if (order.IsFilled)
                    matchedOrders++;
            }
        }

        stopwatch.Stop();
        var tps = processedOrders / stopwatch.Elapsed.TotalSeconds;

        Console.WriteLine($"  [MARKET ORDERS]");
        Console.WriteLine($"    Total Orders: {processedOrders:N0}");
        Console.WriteLine($"    Matched Orders: {matchedOrders:N0}");
        Console.WriteLine($"    Time: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"    TPS: {tps:N0} orders/second");
        Console.WriteLine($"    Order Book - Bid Levels: {engine.Books.BidLevelCount}, Ask Levels: {engine.Books.AskLevelCount}");
    }

    static void TestMixedOrders(int orderCount)
    {
        var engine = new MatchingEngine(
            stepSize: 0.01m,
            pricePrecision: 2,
            makerFeeRate: 0.001m,
            takerFeeRate: 0.002m
        );

        var orders = GenerateMixedOrders(orderCount, "BTCUSDT");
        var stopwatch = Stopwatch.StartNew();
        
        int processedOrders = 0;
        int matchedOrders = 0;
        int limitOrders = 0;
        int marketOrders = 0;

        foreach (var order in orders)
        {
            var result = engine.AddOrder(order, order.Timestamp);
            processedOrders++;
            
            if (order.Price == 0)
                marketOrders++;
            else
                limitOrders++;
            
            if (result == MatchState.OrderAccepted)
            {
                if (order.IsFilled)
                    matchedOrders++;
            }
        }

        stopwatch.Stop();
        var tps = processedOrders / stopwatch.Elapsed.TotalSeconds;

        Console.WriteLine($"  [MIXED ORDERS (Limit + Market)]");
        Console.WriteLine($"    Total Orders: {processedOrders:N0} (Limit: {limitOrders:N0}, Market: {marketOrders:N0})");
        Console.WriteLine($"    Matched Orders: {matchedOrders:N0}");
        Console.WriteLine($"    Time: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"    TPS: {tps:N0} orders/second");
        Console.WriteLine($"    Order Book - Bid Levels: {engine.Books.BidLevelCount}, Ask Levels: {engine.Books.AskLevelCount}");
    }

    static List<Ordering> GenerateLimitOrders(int count, string symbol)
    {
        var orders = new List<Ordering>();
        var random = new Random(42); // Fixed seed for reproducibility
        var basePrice = 50000m; // BTC price

        for (int i = 0; i < count; i++)
        {
            var isBuy = i % 2 == 0; // Alternate between buy and sell
            var priceOffset = (decimal)(random.NextDouble() * 1000 - 500); // ±$500
            var price = Math.Round(basePrice + priceOffset, 2);
            var volume = Math.Round((decimal)(random.NextDouble() * 10 + 0.1), 4); // 0.1 to 10 BTC

            orders.Add(new Ordering
            {
                OrdId = Guid.NewGuid(),
                OrdNo = i + 1,
                Symbol = symbol,
                User = $"user{random.Next(1, 100)}",
                IsBuy = isBuy,
                Price = price,
                Volume = volume,
                Condition = OrderCondition.None,
                Status = OrderStatus.Prepared,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
        }

        return orders;
    }

    static List<Ordering> GenerateMarketOrders(int count, string symbol)
    {
        var orders = new List<Ordering>();
        var random = new Random(43);

        for (int i = 0; i < count; i++)
        {
            var isBuy = i % 2 == 0;
            var volume = Math.Round((decimal)(random.NextDouble() * 5 + 0.1), 4); // 0.1 to 5 BTC

            orders.Add(new Ordering
            {
                OrdId = Guid.NewGuid(),
                OrdNo = i + 1,
                Symbol = symbol,
                User = $"user{random.Next(1, 100)}",
                IsBuy = isBuy,
                Price = 0, // Market order (price = 0 means market order)
                Volume = volume,
                Condition = OrderCondition.IOC, // Market orders typically use IOC
                Status = OrderStatus.Prepared,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
        }

        return orders;
    }

    static List<Ordering> GenerateMixedOrders(int count, string symbol)
    {
        var orders = new List<Ordering>();
        var random = new Random(44);
        var basePrice = 50000m;

        for (int i = 0; i < count; i++)
        {
            var isBuy = i % 2 == 0;
            var isMarketOrder = random.NextDouble() < 0.3; // 30% market orders, 70% limit orders
            
            decimal price;
            OrderCondition condition;

            if (isMarketOrder)
            {
                price = 0; // Market order
                condition = OrderCondition.IOC;
            }
            else
            {
                var priceOffset = (decimal)(random.NextDouble() * 1000 - 500);
                price = Math.Round(basePrice + priceOffset, 2);
                condition = OrderCondition.None;
            }

            var volume = Math.Round((decimal)(random.NextDouble() * 10 + 0.1), 4);

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
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
        }

        return orders;
    }
}
