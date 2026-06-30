using System;
using System.Collections.Generic;

namespace OrderBookTask;

internal static class SelfTests
{
    public static void Run()
    {
        Console.WriteLine("=== Running Self Tests ===");
        
        TestLongStateMap();
        TestOrderBookProcessors();

        Console.WriteLine("=== All Self Tests PASSED ===");
    }

    private static void TestLongStateMap()
    {
        Console.Write("Testing LongStateMap... ");
        
        // 1. Basic operations
        var map = new LongStateMap<ulong>(4); // capacity will be 16 (next power of two greater than 8)
        var reference = new Dictionary<long, ulong>();

        // insert new key
        Assert(map.Count == 0);
        ref var val = ref map.GetValueRefOrAddDefault(10, out bool exists);
        Assert(!exists);
        val = 100;
        reference[10] = 100;
        Assert(map.Count == 1);

        // update existing key
        val = ref map.GetValueRefOrAddDefault(10, out exists);
        Assert(exists);
        Assert(val == 100);
        val = 101;
        reference[10] = 101;

        // insert another key
        ref var val2 = ref map.GetValueRefOrAddDefault(20, out exists);
        Assert(!exists);
        val2 = 200;
        reference[20] = 200;
        Assert(map.Count == 2);

        // remove existing key
        bool removed = map.Remove(10, out ulong oldVal);
        Assert(removed);
        Assert(oldVal == 101);
        reference.Remove(10);
        Assert(map.Count == 1);

        // remove missing key
        removed = map.Remove(10, out oldVal);
        Assert(!removed);
        Assert(oldVal == 0);

        // insert after tombstone
        ref var val3 = ref map.GetValueRefOrAddDefault(10, out exists);
        Assert(!exists);
        val3 = 102;
        reference[10] = 102;
        Assert(map.Count == 2);

        // clear after inserts
        map.Clear();
        reference.Clear();
        Assert(map.Count == 0);

        // reinsert same key after clear
        ref var val4 = ref map.GetValueRefOrAddDefault(10, out exists);
        Assert(!exists);
        val4 = 103;
        reference[10] = 103;
        Assert(map.Count == 1);

        // check that old keys are not visible
        map.Remove(20, out oldVal);
        Assert(oldVal == 0);

        // 2. Probe chain validation
        // Force keys to hash to the same bucket to test probe chain and tombstones
        map.Clear();
        // Since capacity is 16, mask is 15.
        // We'll generate keys that hash to the same index by finding keys that produce same (hash & 15).
        // LongStateMap uses simple Hash: return (int)(x ^ (x >> 32))
        // So any keys with same lowest 4 bits will hash to same bucket if they are < 2^32.
        long key1 = 1;
        long key2 = 17;
        long key3 = 33;

        ref var v1 = ref map.GetValueRefOrAddDefault(key1, out exists); v1 = 111;
        ref var v2 = ref map.GetValueRefOrAddDefault(key2, out exists); v2 = 222;
        ref var v3 = ref map.GetValueRefOrAddDefault(key3, out exists); v3 = 333;

        // Delete key2 (middle of the probe chain)
        removed = map.Remove(key2, out oldVal);
        Assert(removed);
        Assert(oldVal == 222);

        // key3 should still be found (probe chain survives delete)
        ref var v3Find = ref map.GetValueRefOrAddDefault(key3, out exists);
        Assert(exists);
        Assert(v3Find == 333);

        // 3. Random operations comparison
        map = new LongStateMap<ulong>(100);
        reference.Clear();
        var rng = new Random(42);

        for (int i = 0; i < 5000; i++)
        {
            int op = rng.Next(4);
            long key = rng.Next(100);
            ulong value = (ulong)rng.Next(1000) + 1;

            if (op == 0) // Insert/Update
            {
                ref var rVal = ref map.GetValueRefOrAddDefault(key, out exists);
                bool refExists = reference.TryGetValue(key, out ulong refVal);
                Assert(exists == refExists);
                if (exists)
                {
                    Assert(rVal == refVal);
                }
                rVal = value;
                reference[key] = value;
            }
            else if (op == 1) // Remove
            {
                bool rRemoved = map.Remove(key, out ulong rOld);
                bool refRemoved = reference.Remove(key, out ulong refOld);
                Assert(rRemoved == refRemoved);
                if (rRemoved)
                {
                    Assert(rOld == refOld);
                }
            }
            else if (op == 2) // Clear
            {
                if (rng.Next(20) == 0)
                {
                    map.Clear();
                    reference.Clear();
                }
            }

            Assert(map.Count == reference.Count);
        }

        Console.WriteLine("PASSED");
    }

    private static void TestOrderBookProcessors()
    {
        Console.Write("Testing OrderBookProcessors against naive reference... ");

        var naive = new NaiveProcessor();
        var maxPrice = 10000;
        var capacity = 1000;
        var optProcessor = new OrderBookProcessor(maxPrice, capacity);
        var fullProcessor = new FullOrderBookProcessor(maxPrice, capacity);

        var rng = new Random(1337);
        var activeOrderIds = new List<long>();

        // Generate synthetic tick sequences and compare results
        for (int step = 0; step < 20; step++)
        {
            int tickCount = 100;
            var ticks = new Tick[tickCount];
            var bestBidOpt = new int[tickCount];
            var bestAskOpt = new int[tickCount];
            var resultsFull = new FullResultRow[tickCount];

            activeOrderIds.Clear();
            naive.Reset();
            optProcessor.Reset();
            fullProcessor.Reset();

            for (int i = 0; i < tickCount; i++)
            {
                byte action;
                long orderId;
                byte side = rng.Next(2) == 0 ? Constants.SideBid : Constants.SideAsk;
                int price = rng.Next(1, 1000); // price 0 is also tested in specialized test
                int qty = rng.Next(1, 100);

                if (activeOrderIds.Count == 0 || rng.Next(10) < 6)
                {
                    // Add
                    action = Constants.ActionAdd;
                    orderId = rng.Next(1000000);
                    if (!activeOrderIds.Contains(orderId))
                    {
                        activeOrderIds.Add(orderId);
                    }
                }
                else
                {
                    orderId = activeOrderIds[rng.Next(activeOrderIds.Count)];
                    int actionRng = rng.Next(10);
                    if (actionRng < 4)
                    {
                        // Modify
                        action = Constants.ActionModify;
                    }
                    else if (actionRng < 8)
                    {
                        // Delete
                        action = Constants.ActionDelete;
                        activeOrderIds.Remove(orderId);
                    }
                    else
                    {
                        // Clear Y/F
                        action = rng.Next(2) == 0 ? Constants.ActionClearY : Constants.ActionClearF;
                        activeOrderIds.Clear();
                    }
                }

                ticks[i] = new Tick(1000 + i, side, action, orderId, price, qty);
            }

            // Test Price 0 edge case specifically on the last tick
            ticks[tickCount - 1] = new Tick(2000, Constants.SideBid, Constants.ActionAdd, 999999, 0, 5);

            // Process with naive
            var expectedB0 = new int[tickCount];
            var expectedA0 = new int[tickCount];
            var expectedBQ0 = new int[tickCount];
            var expectedBN0 = new int[tickCount];
            var expectedAQ0 = new int[tickCount];
            var expectedAN0 = new int[tickCount];

            for (int i = 0; i < tickCount; i++)
            {
                naive.Process(ticks[i]);
                expectedB0[i] = naive.BestBid;
                expectedA0[i] = naive.BestAsk;
                expectedBQ0[i] = naive.BestBidQty;
                expectedBN0[i] = naive.BestBidCount;
                expectedAQ0[i] = naive.BestAskQty;
                expectedAN0[i] = naive.BestAskCount;
            }

            // Process with optimized
            optProcessor.Process(ticks, bestBidOpt, bestAskOpt);

            // Process with full
            fullProcessor.Process(ticks, resultsFull);

            // Validate
            for (int i = 0; i < tickCount; i++)
            {
                Assert(bestBidOpt[i] == expectedB0[i]);
                Assert(bestAskOpt[i] == expectedA0[i]);

                Assert(resultsFull[i].B0 == expectedB0[i]);
                Assert(resultsFull[i].BQ0 == expectedBQ0[i]);
                Assert(resultsFull[i].BN0 == expectedBN0[i]);
                Assert(resultsFull[i].A0 == expectedA0[i]);
                Assert(resultsFull[i].AQ0 == expectedAQ0[i]);
                Assert(resultsFull[i].AN0 == expectedAN0[i]);
            }
        }

        Console.WriteLine("PASSED");
    }

    private static void Assert(bool condition, [System.Runtime.CompilerServices.CallerArgumentExpression("condition")] string expr = "")
    {
        if (!condition)
        {
            throw new Exception($"Assertion failed: {expr}");
        }
    }
}

internal class NaiveProcessor
{
    private readonly Dictionary<long, (byte Side, int Price, int Qty)> _orders = new();
    
    public int BestBid { get; private set; } = -1;
    public int BestAsk { get; private set; } = -1;
    
    public int BestBidQty { get; private set; }
    public int BestBidCount { get; private set; }
    public int BestAskQty { get; private set; }
    public int BestAskCount { get; private set; }

    public void Reset()
    {
        _orders.Clear();
        Recalculate();
    }

    public void Process(Tick tick)
    {
        var action = tick.Action;
        if (action == Constants.ActionAdd || action == Constants.ActionModify)
        {
            _orders[tick.OrderId] = (tick.Side, tick.Price, tick.Qty);
        }
        else if (action == Constants.ActionDelete)
        {
            _orders.Remove(tick.OrderId);
        }
        else if (action == Constants.ActionClearY || action == Constants.ActionClearF)
        {
            _orders.Clear();
        }
        Recalculate();
    }

    private void Recalculate()
    {
        var bidQty = new Dictionary<int, int>();
        var bidCount = new Dictionary<int, int>();
        var askQty = new Dictionary<int, int>();
        var askCount = new Dictionary<int, int>();

        foreach (var order in _orders.Values)
        {
            if (order.Side == Constants.SideBid)
            {
                bidQty[order.Price] = bidQty.GetValueOrDefault(order.Price) + order.Qty;
                bidCount[order.Price] = bidCount.GetValueOrDefault(order.Price) + 1;
            }
            else if (order.Side == Constants.SideAsk)
            {
                askQty[order.Price] = askQty.GetValueOrDefault(order.Price) + order.Qty;
                askCount[order.Price] = askCount.GetValueOrDefault(order.Price) + 1;
            }
        }

        BestBid = -1;
        BestBidQty = 0;
        BestBidCount = 0;
        foreach (var p in bidCount.Keys)
        {
            if (p > BestBid && bidCount[p] > 0)
            {
                BestBid = p;
                BestBidQty = bidQty[p];
                BestBidCount = bidCount[p];
            }
        }

        BestAsk = -1;
        BestAskQty = 0;
        BestAskCount = 0;
        foreach (var p in askCount.Keys)
        {
            if (p > -1 && (BestAsk == -1 || p < BestAsk) && askCount[p] > 0)
            {
                BestAsk = p;
                BestAskQty = askQty[p];
                BestAskCount = askCount[p];
            }
        }
    }
}
