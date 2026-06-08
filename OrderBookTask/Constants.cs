namespace OrderBookTask;

internal static class Constants
{
    public const int RecordSizeBytes = 26;
    public const int EmptyPriceSentinel = -1;
    public const int MaxAllowedPrice = 2_000_000;

    public const byte SideBid = (byte)'1';
    public const byte SideAsk = (byte)'2';
    public const byte SideEmpty = 0;

    public const byte ActionClearY = (byte)'Y';
    public const byte ActionClearF = (byte)'F';
    public const byte ActionAdd = (byte)'A';
    public const byte ActionModify = (byte)'M';
    public const byte ActionDelete = (byte)'D';

    public const string OutputHeader = "SourceTime;Side;Action;OrderId;Price;Qty;B0;BQ0;BN0;A0;AQ0;AN0";
}
