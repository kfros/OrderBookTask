using System.Buffers.Binary;

namespace OrderBookTask;

internal sealed class RawTickReader
{
    public ReadResult Read(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);
        if (bytes.Length % Constants.RecordSizeBytes != 0)
        {
            throw new InvalidDataException(
                $"Input file size {bytes.Length} is not divisible by record size {Constants.RecordSizeBytes}.");
        }

        var tickCount = bytes.Length / Constants.RecordSizeBytes;
        var ticks = new Tick[tickCount];
        var maxPrice = Constants.EmptyPriceSentinel;

        for (var i = 0; i < tickCount; i++)
        {
            var offset = i * Constants.RecordSizeBytes;
            var sourceTime = BinaryPrimitives.ReadInt64BigEndian(bytes.AsSpan(offset, 8));
            var side = bytes[offset + 8];
            var action = bytes[offset + 9];
            var orderId = BinaryPrimitives.ReadInt64BigEndian(bytes.AsSpan(offset + 10, 8));
            var price = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset + 18, 4));
            var qty = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset + 22, 4));

            if (price < 0)
            {
                throw new InvalidDataException($"Negative price {price} is invalid.");
            }

            ticks[i] = new Tick(sourceTime, side, action, orderId, price, qty);
            if (price > maxPrice)
            {
                maxPrice = price;
            }
        }

        return new ReadResult(ticks, maxPrice, tickCount);
    }
}
