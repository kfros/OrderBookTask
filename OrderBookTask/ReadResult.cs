namespace OrderBookTask;

internal readonly struct ReadResult
{
    public ReadResult(Tick[] ticks, int maxPrice, int tickCount)
    {
        Ticks = ticks;
        MaxPrice = maxPrice;
        TickCount = tickCount;
    }

    public Tick[] Ticks { get; }
    public int MaxPrice { get; }
    public int TickCount { get; }
}
