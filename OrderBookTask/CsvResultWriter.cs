using System.Globalization;

namespace OrderBookTask;

internal sealed class CsvResultWriter
{
    public void Write(string outputPath, Tick[] ticks, int[] bestBidByTick, int[] bestAskByTick)
    {
        using var writer = new StreamWriter(outputPath);
        writer.WriteLine(Constants.OutputHeader);

        for (var i = 0; i < ticks.Length; i++)
        {
            var tick = ticks[i];
            writer.Write(tick.SourceTime.ToString(CultureInfo.InvariantCulture));
            writer.Write(';');
            writer.Write(FormatByte(tick.Side));
            writer.Write(';');
            writer.Write(FormatByte(tick.Action));
            writer.Write(';');
            writer.Write(tick.OrderId.ToString(CultureInfo.InvariantCulture));
            writer.Write(';');
            writer.Write(tick.Price.ToString(CultureInfo.InvariantCulture));
            writer.Write(';');
            writer.Write(tick.Qty.ToString(CultureInfo.InvariantCulture));
            writer.Write(';');
            writer.Write(FormatPrice(bestBidByTick[i]));
            writer.Write(";;;");
            writer.Write(FormatPrice(bestAskByTick[i]));
            writer.Write(";;");
            writer.WriteLine();
        }
    }

    public void WriteFull(string outputPath, Tick[] ticks, FullResultRow[] results)
    {
        using var writer = new StreamWriter(outputPath);
        writer.WriteLine(Constants.OutputHeader);

        for (var i = 0; i < ticks.Length; i++)
        {
            var tick = ticks[i];
            writer.Write(tick.SourceTime.ToString(CultureInfo.InvariantCulture));
            writer.Write(';');
            writer.Write(FormatByte(tick.Side));
            writer.Write(';');
            writer.Write(FormatByte(tick.Action));
            writer.Write(';');
            writer.Write(tick.OrderId.ToString(CultureInfo.InvariantCulture));
            writer.Write(';');
            writer.Write(tick.Price.ToString(CultureInfo.InvariantCulture));
            writer.Write(';');
            writer.Write(tick.Qty.ToString(CultureInfo.InvariantCulture));
            writer.Write(';');

            var b0 = results[i].B0;
            if (b0 == Constants.EmptyPriceSentinel)
            {
                writer.Write(";;;");
            }
            else
            {
                writer.Write(b0.ToString(CultureInfo.InvariantCulture));
                writer.Write(';');
                writer.Write(results[i].BQ0.ToString(CultureInfo.InvariantCulture));
                writer.Write(';');
                writer.Write(results[i].BN0.ToString(CultureInfo.InvariantCulture));
                writer.Write(';');
            }

            var a0 = results[i].A0;
            if (a0 == Constants.EmptyPriceSentinel)
            {
                writer.Write(";;");
            }
            else
            {
                writer.Write(a0.ToString(CultureInfo.InvariantCulture));
                writer.Write(';');
                writer.Write(results[i].AQ0.ToString(CultureInfo.InvariantCulture));
                writer.Write(';');
                writer.Write(results[i].AN0.ToString(CultureInfo.InvariantCulture));
            }
            writer.WriteLine();
        }
    }

    private static string FormatByte(byte value) => value == 0 ? string.Empty : ((char)value).ToString();

    private static string FormatPrice(int price) =>
        price == Constants.EmptyPriceSentinel ? string.Empty : price.ToString(CultureInfo.InvariantCulture);
}
