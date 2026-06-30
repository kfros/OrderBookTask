using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OrderBookTask;

internal sealed class FullOrderBookProcessor
{
    private readonly LongStateMap<ulong> _activeOrders;
    private readonly int[] _bidCountByPrice;
    private readonly int[] _bidQtyByPrice;
    private readonly int[] _askCountByPrice;
    private readonly int[] _askQtyByPrice;
    private readonly int[] _touchedBidPrices;
    private readonly int[] _touchedAskPrices;
    private readonly int _maxPrice;
    private int _touchedBidCount;
    private int _touchedAskCount;
    private int _bestBid;
    private int _bestAsk;

    public FullOrderBookProcessor(int maxPrice, int expectedOrderCapacity)
    {
        _maxPrice = maxPrice;
        _activeOrders = new LongStateMap<ulong>(expectedOrderCapacity);
        _bidCountByPrice = new int[maxPrice + 1];
        _bidQtyByPrice = new int[maxPrice + 1];
        _askCountByPrice = new int[maxPrice + 1];
        _askQtyByPrice = new int[maxPrice + 1];
        _touchedBidPrices = new int[expectedOrderCapacity];
        _touchedAskPrices = new int[expectedOrderCapacity];
        _touchedBidCount = 0;
        _touchedAskCount = 0;
        _bestBid = -1;
        _bestAsk = -1;
    }

    public void Reset()
    {
        ClearBook();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ClearBook()
    {
        _activeOrders.Clear();
        for (var i = 0; i < _touchedBidCount; i++)
        {
            var p = _touchedBidPrices[i];
            _bidCountByPrice[p] = 0;
            _bidQtyByPrice[p] = 0;
        }
        _touchedBidCount = 0;

        for (var i = 0; i < _touchedAskCount; i++)
        {
            var p = _touchedAskPrices[i];
            _askCountByPrice[p] = 0;
            _askQtyByPrice[p] = 0;
        }
        _touchedAskCount = 0;

        _bestBid = -1;
        _bestAsk = -1;
    }

    public void Process(Tick[] ticks, FullResultRow[] results)
    {
        var bidQty = _bidQtyByPrice;
        var bidCount = _bidCountByPrice;
        var askQty = _askQtyByPrice;
        var askCount = _askCountByPrice;

        for (var i = 0; i < ticks.Length; i++)
        {
            ref readonly var tick = ref ticks[i];
            var action = tick.Action;

            if (action == Constants.ActionAdd)
            {
                ProcessUpsert(tick.OrderId, tick.Side, tick.Price, tick.Qty);
            }
            else if (action == Constants.ActionDelete)
            {
                ProcessDelete(tick.OrderId);
            }
            else if (action == Constants.ActionModify)
            {
                ProcessUpsert(tick.OrderId, tick.Side, tick.Price, tick.Qty);
            }
            else if (action == Constants.ActionClearY || action == Constants.ActionClearF)
            {
                ClearBook();
            }

            int bb = _bestBid;
            if (bb == -1)
            {
                results[i].B0 = -1;
                results[i].BQ0 = 0;
                results[i].BN0 = 0;
            }
            else
            {
                results[i].B0 = bb;
                results[i].BQ0 = bidQty[bb];
                results[i].BN0 = bidCount[bb];
            }

            int ba = _bestAsk;
            if (ba == -1)
            {
                results[i].A0 = -1;
                results[i].AQ0 = 0;
                results[i].AN0 = 0;
            }
            else
            {
                results[i].A0 = ba;
                results[i].AQ0 = askQty[ba];
                results[i].AN0 = askCount[ba];
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessUpsert(long orderId, byte newSide, int newPrice, int newQty)
    {
        ref var stateRef = ref _activeOrders.GetValueRefOrAddDefault(orderId, out var exists);
        if (exists)
        {
            var oldState = stateRef;
            var isAsk = StatePacker.IsFullAsk(oldState);
            var oldPrice = StatePacker.GetFullPrice(oldState);
            var oldQty = StatePacker.GetFullQty(oldState);

            var needRepairOldBid = false;
            var needRepairOldAsk = false;

            if (!isAsk)
            {
                _bidCountByPrice[oldPrice]--;
                _bidQtyByPrice[oldPrice] -= oldQty;
                if (_bidCountByPrice[oldPrice] == 0 && oldPrice == _bestBid)
                {
                    needRepairOldBid = true;
                }
            }
            else
            {
                _askCountByPrice[oldPrice]--;
                _askQtyByPrice[oldPrice] -= oldQty;
                if (_askCountByPrice[oldPrice] == 0 && oldPrice == _bestAsk)
                {
                    needRepairOldAsk = true;
                }
            }

            stateRef = StatePacker.PackFullState(newSide, newPrice, newQty);

            if (newSide == Constants.SideBid)
            {
                if (_bidCountByPrice[newPrice] == 0)
                {
                    _touchedBidPrices[_touchedBidCount++] = newPrice;
                }
                _bidCountByPrice[newPrice]++;
                _bidQtyByPrice[newPrice] += newQty;
                if (newPrice > _bestBid)
                {
                    _bestBid = newPrice;
                }
            }
            else if (newSide == Constants.SideAsk)
            {
                if (_askCountByPrice[newPrice] == 0)
                {
                    _touchedAskPrices[_touchedAskCount++] = newPrice;
                }
                _askCountByPrice[newPrice]++;
                _askQtyByPrice[newPrice] += newQty;
                if (_bestAsk == -1 || newPrice < _bestAsk)
                {
                    _bestAsk = newPrice;
                }
            }

            if (needRepairOldBid)
            {
                if (_bestBid == oldPrice && _bidCountByPrice[oldPrice] == 0)
                {
                    var p = oldPrice - 1;
                    while (p >= 0 && _bidCountByPrice[p] == 0)
                    {
                        p--;
                    }
                    _bestBid = p;
                }
            }

            if (needRepairOldAsk)
            {
                if (_bestAsk == oldPrice && _askCountByPrice[oldPrice] == 0)
                {
                    var p = oldPrice + 1;
                    while (p <= _maxPrice && _askCountByPrice[p] == 0)
                    {
                        p++;
                    }
                    _bestAsk = (p > _maxPrice) ? -1 : p;
                }
            }
        }
        else
        {
            stateRef = StatePacker.PackFullState(newSide, newPrice, newQty);

            if (newSide == Constants.SideBid)
            {
                if (_bidCountByPrice[newPrice] == 0)
                {
                    _touchedBidPrices[_touchedBidCount++] = newPrice;
                }
                _bidCountByPrice[newPrice]++;
                _bidQtyByPrice[newPrice] += newQty;
                if (newPrice > _bestBid)
                {
                    _bestBid = newPrice;
                }
            }
            else if (newSide == Constants.SideAsk)
            {
                if (_askCountByPrice[newPrice] == 0)
                {
                    _touchedAskPrices[_touchedAskCount++] = newPrice;
                }
                _askCountByPrice[newPrice]++;
                _askQtyByPrice[newPrice] += newQty;
                if (_bestAsk == -1 || newPrice < _bestAsk)
                {
                    _bestAsk = newPrice;
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessDelete(long orderId)
    {
        if (!_activeOrders.Remove(orderId, out var state))
        {
            return;
        }

        var isAsk = StatePacker.IsFullAsk(state);
        var price = StatePacker.GetFullPrice(state);
        var qty = StatePacker.GetFullQty(state);

        if (!isAsk)
        {
            _bidCountByPrice[price]--;
            _bidQtyByPrice[price] -= qty;
            if (_bidCountByPrice[price] == 0 && price == _bestBid)
            {
                var p = price - 1;
                while (p >= 0 && _bidCountByPrice[p] == 0)
                {
                    p--;
                }
                _bestBid = p;
            }
        }
        else
        {
            _askCountByPrice[price]--;
            _askQtyByPrice[price] -= qty;
            if (_askCountByPrice[price] == 0 && price == _bestAsk)
            {
                var p = price + 1;
                while (p <= _maxPrice && _askCountByPrice[p] == 0)
                {
                    p++;
                }
                _bestAsk = (p > _maxPrice) ? -1 : p;
            }
        }
    }
}
