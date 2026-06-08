namespace OrderBookTask;

internal readonly struct Tick
{
    public Tick(long sourceTime, byte side, byte action, long orderId, int price, int qty)
    {
        SourceTime = sourceTime;
        Side = side;
        Action = action;
        OrderId = orderId;
        Price = price;
        Qty = qty;
    }

    public long SourceTime { get; }
    public byte Side { get; }
    public byte Action { get; }
    public long OrderId { get; }
    public int Price { get; }
    public int Qty { get; }
}
