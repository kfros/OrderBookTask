using System;
using System.IO;

namespace OrderBookTask;

internal sealed class BookInvariantValidator
{
    public int ValidateValidBookInterval(Tick[] ticks, int[] bestBidByTick, int[] bestAskByTick)
    {
        int checkedTicks = 0;
        for (int i = 0; i < ticks.Length; i++)
        {
            long sourceTime = ticks[i].SourceTime;
            if (sourceTime >= Constants.ValidBookSourceTimeFrom && sourceTime <= Constants.ValidBookSourceTimeTo)
            {
                int b0 = bestBidByTick[i];
                int a0 = bestAskByTick[i];

                if (b0 == Constants.EmptyPriceSentinel)
                {
                    throw new InvalidDataException(
                        $"Book invariant failure at tick index {i}, SourceTime {sourceTime}: B0 is missing. expected invariant: B0 < A0");
                }
                if (a0 == Constants.EmptyPriceSentinel)
                {
                    throw new InvalidDataException(
                        $"Book invariant failure at tick index {i}, SourceTime {sourceTime}: A0 is missing. expected invariant: B0 < A0");
                }
                if (b0 >= a0)
                {
                    throw new InvalidDataException(
                        $"Book invariant failure at tick index {i}, SourceTime {sourceTime}: B0 ({b0}) >= A0 ({a0}). expected invariant: B0 < A0");
                }
                checkedTicks++;
            }
        }

        if (checkedTicks == 0)
        {
            throw new InvalidDataException(
                $"Book invariant failure: no ticks checked within source time interval [{Constants.ValidBookSourceTimeFrom}, {Constants.ValidBookSourceTimeTo}].");
        }

        return checkedTicks;
    }

    public int ValidateValidBookInterval(Tick[] ticks, FullResultRow[] results)
    {
        int checkedTicks = 0;
        for (int i = 0; i < ticks.Length; i++)
        {
            long sourceTime = ticks[i].SourceTime;
            if (sourceTime >= Constants.ValidBookSourceTimeFrom && sourceTime <= Constants.ValidBookSourceTimeTo)
            {
                int b0 = results[i].B0;
                int a0 = results[i].A0;

                if (b0 == Constants.EmptyPriceSentinel)
                {
                    throw new InvalidDataException(
                        $"Book invariant failure at tick index {i}, SourceTime {sourceTime}: B0 is missing. expected invariant: B0 < A0");
                }
                if (a0 == Constants.EmptyPriceSentinel)
                {
                    throw new InvalidDataException(
                        $"Book invariant failure at tick index {i}, SourceTime {sourceTime}: A0 is missing. expected invariant: B0 < A0");
                }
                if (b0 >= a0)
                {
                    throw new InvalidDataException(
                        $"Book invariant failure at tick index {i}, SourceTime {sourceTime}: B0 ({b0}) >= A0 ({a0}). expected invariant: B0 < A0");
                }
                checkedTicks++;
            }
        }

        if (checkedTicks == 0)
        {
            throw new InvalidDataException(
                $"Book invariant failure: no ticks checked within source time interval [{Constants.ValidBookSourceTimeFrom}, {Constants.ValidBookSourceTimeTo}].");
        }

        return checkedTicks;
    }
}
