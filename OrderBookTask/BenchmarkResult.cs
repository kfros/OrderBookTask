namespace OrderBookTask;

internal sealed class BenchmarkResult
{
    public BenchmarkResult(TimeSpan[] measuredRuns, int tickCount)
    {
        MeasuredRuns = measuredRuns;
        TickCount = tickCount;
    }

    public TimeSpan[] MeasuredRuns { get; }
    public int TickCount { get; }

    public TimeSpan BestElapsed => MeasuredRuns.Length == 0
        ? TimeSpan.Zero
        : MeasuredRuns.Min();

    public double BestMicrosecondsPerTick => TickCount == 0
        ? 0
        : BestElapsed.TotalMicroseconds / TickCount;

    public double BestNanosecondsPerTick => BestMicrosecondsPerTick * 1_000;
}
