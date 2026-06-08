namespace OrderBookTask;

internal sealed class OrderBookProcessor
{
    private readonly int _maxPrice;
    private readonly int _expectedOrderCapacity;

    public OrderBookProcessor(int maxPrice, int expectedOrderCapacity)
    {
        _maxPrice = maxPrice;
        _expectedOrderCapacity = expectedOrderCapacity;
    }

    public void Reset()
    {
        // TODO: Clear future price-level count arrays and OrderId index.
        // Reset must not depend on the first input tick being F/Y.
    }

    public void Process(Tick[] ticks, int[] bestBidByTick, int[] bestAskByTick)
    {
        _ = ticks;
        _ = bestBidByTick;
        _ = bestAskByTick;
        _ = _maxPrice;
        _ = _expectedOrderCapacity;

        // TODO: Implement optimized B0/A0-only build in the next phase.
        // Do not compute BQ0, BN0, AQ0, or AN0 here.
        throw new NotImplementedException("Order book hot-path processing is intentionally not implemented in this scaffold.");
    }
}
