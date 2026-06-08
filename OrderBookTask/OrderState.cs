namespace OrderBookTask;

internal struct OrderState
{
    public OrderState(byte side, int price)
    {
        Side = side;
        Price = price;
    }

    public byte Side;
    public int Price;
}
