using System;

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

    public TimeSpan BestElapsed
    {
        get
        {
            if (MeasuredRuns == null || MeasuredRuns.Length == 0)
            {
                return TimeSpan.Zero;
            }

            var min = MeasuredRuns[0];
            for (var i = 1; i < MeasuredRuns.Length; i++)
            {
                if (MeasuredRuns[i] < min)
                {
                    min = MeasuredRuns[i];
                }
            }
            return min;
        }
    }

    public double BestMicroseconds => BestElapsed.TotalMicroseconds;

    public double BestMicrosecondsPerTick => TickCount == 0
        ? 0
        : BestElapsed.TotalMicroseconds / TickCount;

    public double BestNanosecondsPerTick => BestMicrosecondsPerTick * 1_000;
}
