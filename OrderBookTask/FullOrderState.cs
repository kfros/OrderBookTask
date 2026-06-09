namespace OrderBookTask;

internal struct FullOrderState
{
    public FullOrderState(byte side, int price, int qty)
    {
        Side = side;
        Price = price;
        Qty = qty;
    }

    public byte Side;
    public int Price;
    public int Qty;
}
