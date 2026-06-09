using System;

namespace OrderBookTask;

internal sealed class FullResultArrays
{
    public FullResultArrays(int tickCount)
    {
        BestBidByTick = new int[tickCount];
        BestBidQtyByTick = new int[tickCount];
        BestBidCountByTick = new int[tickCount];
        BestAskByTick = new int[tickCount];
        BestAskQtyByTick = new int[tickCount];
        BestAskCountByTick = new int[tickCount];

        Array.Fill(BestBidByTick, Constants.EmptyPriceSentinel);
        Array.Fill(BestAskByTick, Constants.EmptyPriceSentinel);
    }

    public int[] BestBidByTick { get; }
    public int[] BestBidQtyByTick { get; }
    public int[] BestBidCountByTick { get; }
    public int[] BestAskByTick { get; }
    public int[] BestAskQtyByTick { get; }
    public int[] BestAskCountByTick { get; }
}
