namespace OrderBookTask;

internal sealed class SampleValidator
{
    public void ValidateDecodedInput(string sampleInputPath, Tick[] ticks, int rowCount)
    {
        _ = sampleInputPath;
        _ = ticks;
        _ = rowCount;

        // TODO: Compare SourceTime, Side, Action, OrderId, Price, and Qty against ticks_sample.csv.
        // This validation is outside the measured benchmark scope.
        throw new NotImplementedException("Decoded input validation is reserved for the implementation phase.");
    }

    public void ValidateOptimizedResult(
        string sampleResultPath,
        Tick[] ticks,
        int[] bestBidByTick,
        int[] bestAskByTick,
        int rowCount)
    {
        _ = sampleResultPath;
        _ = ticks;
        _ = bestBidByTick;
        _ = bestAskByTick;
        _ = rowCount;

        // TODO: Compare original fields plus B0 and A0 only.
        // Ignore BQ0, BN0, AQ0, and AN0 in optimized mode validation.
        throw new NotImplementedException("Optimized result validation is reserved for the implementation phase.");
    }
}
