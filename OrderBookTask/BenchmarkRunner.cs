using System.Diagnostics;

namespace OrderBookTask;

internal sealed class BenchmarkRunner
{
    public BenchmarkResult Run(
        OrderBookProcessor processor,
        Tick[] ticks,
        int[] bestBidByTick,
        int[] bestAskByTick,
        int warmupRuns,
        int measuredRuns)
    {
        for (var i = 0; i < warmupRuns; i++)
        {
            processor.Reset();
            processor.Process(ticks, bestBidByTick, bestAskByTick);
        }

        var timings = new TimeSpan[measuredRuns];
        for (var i = 0; i < measuredRuns; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            processor.Reset();
            processor.Process(ticks, bestBidByTick, bestAskByTick);
            stopwatch.Stop();

            timings[i] = stopwatch.Elapsed;
        }

        return new BenchmarkResult(timings, ticks.Length);
    }

    public BenchmarkResult Run(
        FullOrderBookProcessor processor,
        Tick[] ticks,
        FullResultRow[] results,
        int warmupRuns,
        int measuredRuns)
    {
        for (var i = 0; i < warmupRuns; i++)
        {
            processor.Reset();
            processor.Process(ticks, results);
        }

        var timings = new TimeSpan[measuredRuns];
        for (var i = 0; i < measuredRuns; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            processor.Reset();
            processor.Process(ticks, results);
            stopwatch.Stop();

            timings[i] = stopwatch.Elapsed;
        }

        return new BenchmarkResult(timings, ticks.Length);
    }
}
