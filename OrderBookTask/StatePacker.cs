using System.Runtime.CompilerServices;

namespace OrderBookTask;

internal static class StatePacker
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PackOptimizedState(byte side, int price)
    {
        return (price << 1) | (side == Constants.SideAsk ? 1 : 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetOptimizedPrice(int packed)
    {
        return packed >> 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOptimizedAsk(int packed)
    {
        return (packed & 1) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong PackFullState(byte side, int price, int qty)
    {
        ulong sideBit = (side == Constants.SideAsk) ? 1UL : 0UL;
        ulong pricePart = ((ulong)price) << 1;
        ulong qtyPart = ((ulong)qty) << 32;
        return qtyPart | pricePart | sideBit;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetFullPrice(ulong packed)
    {
        return (int)((packed >> 1) & 0x7FFFFFFFUL);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetFullQty(ulong packed)
    {
        return (int)(packed >> 32);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFullAsk(ulong packed)
    {
        return (packed & 1) != 0;
    }
}
