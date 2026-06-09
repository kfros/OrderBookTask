using System.IO;

namespace OrderBookTask;

internal sealed class SampleValidator
{
    public void ValidateDecodedInput(string sampleInputPath, Tick[] ticks, int rowCount)
    {
        using var reader = new StreamReader(sampleInputPath);
        string? header = reader.ReadLine();
        if (header == null)
        {
            throw new InvalidDataException("Sample input file is empty.");
        }

        for (int i = 0; i < rowCount; i++)
        {
            string? line = reader.ReadLine();
            if (line == null)
            {
                throw new InvalidDataException($"Expected at least {rowCount} data rows, but file ended early.");
            }

            var parts = line.Split(';');
            if (parts.Length < 6)
            {
                throw new InvalidDataException($"Invalid line {i + 2} in sample input: {line}");
            }

            long sourceTime = long.Parse(parts[0]);
            byte side = string.IsNullOrEmpty(parts[1]) ? (byte)0 : (byte)parts[1][0];
            byte action = string.IsNullOrEmpty(parts[2]) ? (byte)0 : (byte)parts[2][0];
            long orderId = long.Parse(parts[3]);
            int price = int.Parse(parts[4]);
            int qty = int.Parse(parts[5]);

            var tick = ticks[i];
            if (tick.SourceTime != sourceTime)
                throw new InvalidDataException($"Mismatch at index {i} (row {i + 2}): SourceTime expected {sourceTime}, got {tick.SourceTime}");
            if (tick.Side != side)
                throw new InvalidDataException($"Mismatch at index {i} (row {i + 2}): Side expected {side}, got {tick.Side}");
            if (tick.Action != action)
                throw new InvalidDataException($"Mismatch at index {i} (row {i + 2}): Action expected {action}, got {tick.Action}");
            if (tick.OrderId != orderId)
                throw new InvalidDataException($"Mismatch at index {i} (row {i + 2}): OrderId expected {orderId}, got {tick.OrderId}");
            if (tick.Price != price)
                throw new InvalidDataException($"Mismatch at index {i} (row {i + 2}): Price expected {price}, got {tick.Price}");
            if (tick.Qty != qty)
                throw new InvalidDataException($"Mismatch at index {i} (row {i + 2}): Qty expected {qty}, got {tick.Qty}");
        }
    }

    public void ValidateOptimizedResult(
        string sampleResultPath,
        Tick[] ticks,
        int[] bestBidByTick,
        int[] bestAskByTick,
        int rowCount)
    {
        using var reader = new StreamReader(sampleResultPath);
        string? header = reader.ReadLine();
        if (header == null)
        {
            throw new InvalidDataException("Sample result file is empty.");
        }

        for (int i = 0; i < rowCount; i++)
        {
            string? line = reader.ReadLine();
            if (line == null)
            {
                throw new InvalidDataException($"Expected at least {rowCount} data rows, but file ended early.");
            }

            var parts = line.Split(';');
            if (parts.Length < 12)
            {
                throw new InvalidDataException($"Invalid line {i + 2} in sample result: {line}");
            }

            long sourceTime = long.Parse(parts[0]);
            byte side = string.IsNullOrEmpty(parts[1]) ? (byte)0 : (byte)parts[1][0];
            byte action = string.IsNullOrEmpty(parts[2]) ? (byte)0 : (byte)parts[2][0];
            long orderId = long.Parse(parts[3]);
            int price = int.Parse(parts[4]);
            int qty = int.Parse(parts[5]);

            int b0 = string.IsNullOrEmpty(parts[6]) ? -1 : int.Parse(parts[6]);
            int a0 = string.IsNullOrEmpty(parts[9]) ? -1 : int.Parse(parts[9]);

            var tick = ticks[i];
            if (tick.SourceTime != sourceTime)
                throw new InvalidDataException($"Mismatch at index {i} (row {i + 2}): SourceTime expected {sourceTime}, got {tick.SourceTime}");
            if (tick.Side != side)
                throw new InvalidDataException($"Mismatch at index {i} (row {i + 2}): Side expected {side}, got {tick.Side}");
            if (tick.Action != action)
                throw new InvalidDataException($"Mismatch at index {i} (row {i + 2}): Action expected {action}, got {tick.Action}");
            if (tick.OrderId != orderId)
                throw new InvalidDataException($"Mismatch at index {i} (row {i + 2}): OrderId expected {orderId}, got {tick.OrderId}");
            if (tick.Price != price)
                throw new InvalidDataException($"Mismatch at index {i} (row {i + 2}): Price expected {price}, got {tick.Price}");
            if (tick.Qty != qty)
                throw new InvalidDataException($"Mismatch at index {i} (row {i + 2}): Qty expected {qty}, got {tick.Qty}");

            if (bestBidByTick[i] != b0)
                throw new InvalidDataException($"Mismatch at index {i} (row {i + 2}): B0 expected {b0}, got {bestBidByTick[i]}");
            if (bestAskByTick[i] != a0)
                throw new InvalidDataException($"Mismatch at index {i} (row {i + 2}): A0 expected {a0}, got {bestAskByTick[i]}");
        }
    }

    public void ValidateFullResult(
        string sampleResultPath,
        Tick[] ticks,
        FullResultArrays results,
        int rowCount)
    {
        using var reader = new StreamReader(sampleResultPath);
        string? header = reader.ReadLine();
        if (header == null)
        {
            throw new InvalidDataException("Sample result file is empty.");
        }

        for (int i = 0; i < rowCount; i++)
        {
            string? line = reader.ReadLine();
            if (line == null)
            {
                throw new InvalidDataException($"Expected at least {rowCount} data rows, but file ended early.");
            }

            var parts = line.Split(';');
            if (parts.Length < 12)
            {
                throw new InvalidDataException($"Invalid line {i + 2} in sample result: {line}");
            }

            long sourceTime = long.Parse(parts[0]);
            byte side = string.IsNullOrEmpty(parts[1]) ? (byte)0 : (byte)parts[1][0];
            byte action = string.IsNullOrEmpty(parts[2]) ? (byte)0 : (byte)parts[2][0];
            long orderId = long.Parse(parts[3]);
            int price = int.Parse(parts[4]);
            int qty = int.Parse(parts[5]);

            int b0 = string.IsNullOrEmpty(parts[6]) ? -1 : int.Parse(parts[6]);
            int bq0 = string.IsNullOrEmpty(parts[7]) ? 0 : int.Parse(parts[7]);
            int bn0 = string.IsNullOrEmpty(parts[8]) ? 0 : int.Parse(parts[8]);

            int a0 = string.IsNullOrEmpty(parts[9]) ? -1 : int.Parse(parts[9]);
            int aq0 = string.IsNullOrEmpty(parts[10]) ? 0 : int.Parse(parts[10]);
            int an0 = string.IsNullOrEmpty(parts[11]) ? 0 : int.Parse(parts[11]);

            var tick = ticks[i];
            if (tick.SourceTime != sourceTime)
                throw new InvalidDataException($"Mismatch at index {i} (row {i + 2}): SourceTime expected {sourceTime}, got {tick.SourceTime}");
            if (tick.Side != side)
                throw new InvalidDataException($"Mismatch at index {i} (row {i + 2}): Side expected {side}, got {tick.Side}");
            if (tick.Action != action)
                throw new InvalidDataException($"Mismatch at index {i} (row {i + 2}): Action expected {action}, got {tick.Action}");
            if (tick.OrderId != orderId)
                throw new InvalidDataException($"Mismatch at index {i} (row {i + 2}): OrderId expected {orderId}, got {tick.OrderId}");
            if (tick.Price != price)
                throw new InvalidDataException($"Mismatch at index {i} (row {i + 2}): Price expected {price}, got {tick.Price}");
            if (tick.Qty != qty)
                throw new InvalidDataException($"Mismatch at index {i} (row {i + 2}): Qty expected {qty}, got {tick.Qty}");

            if (results.BestBidByTick[i] != b0)
                throw new InvalidDataException($"Mismatch at index {i} (row {i + 2}): B0 expected {b0}, got {results.BestBidByTick[i]}");
            if (results.BestBidQtyByTick[i] != bq0)
                throw new InvalidDataException($"Mismatch at index {i} (row {i + 2}): BQ0 expected {bq0}, got {results.BestBidQtyByTick[i]}");
            if (results.BestBidCountByTick[i] != bn0)
                throw new InvalidDataException($"Mismatch at index {i} (row {i + 2}): BN0 expected {bn0}, got {results.BestBidCountByTick[i]}");

            if (results.BestAskByTick[i] != a0)
                throw new InvalidDataException($"Mismatch at index {i} (row {i + 2}): A0 expected {a0}, got {results.BestAskByTick[i]}");
            if (results.BestAskQtyByTick[i] != aq0)
                throw new InvalidDataException($"Mismatch at index {i} (row {i + 2}): AQ0 expected {aq0}, got {results.BestAskQtyByTick[i]}");
            if (results.BestAskCountByTick[i] != an0)
                throw new InvalidDataException($"Mismatch at index {i} (row {i + 2}): AN0 expected {an0}, got {results.BestAskCountByTick[i]}");
        }
    }
}
