using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OrderBookTask;

internal sealed class OrderBookProcessor
{
    private readonly LongStateMap<int> _activeOrders;
    private readonly int[] _bidCountByPrice;
    private readonly int[] _askCountByPrice;
    private readonly int[] _touchedBidPrices;
    private readonly int[] _touchedAskPrices;
    private readonly int _maxPrice;
    private int _touchedBidCount;
    private int _touchedAskCount;
    private int _bestBid;
    private int _bestAsk;

    public OrderBookProcessor(int maxPrice, int expectedOrderCapacity)
    {
        _maxPrice = maxPrice;
        _activeOrders = new LongStateMap<int>(expectedOrderCapacity);
        _bidCountByPrice = new int[maxPrice + 1];
        _askCountByPrice = new int[maxPrice + 1];
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
            _bidCountByPrice[_touchedBidPrices[i]] = 0;
        }
        _touchedBidCount = 0;

        for (var i = 0; i < _touchedAskCount; i++)
        {
            _askCountByPrice[_touchedAskPrices[i]] = 0;
        }
        _touchedAskCount = 0;

        _bestBid = -1;
        _bestAsk = -1;
    }

    public void Process(Tick[] ticks, int[] bestBidByTick, int[] bestAskByTick)
    {
        for (var i = 0; i < ticks.Length; i++)
        {
            ref readonly var tick = ref ticks[i];
            var action = tick.Action;

            if (action == Constants.ActionAdd)
            {
                ProcessUpsert(tick.OrderId, tick.Side, tick.Price);
            }
            else if (action == Constants.ActionDelete)
            {
                ProcessDelete(tick.OrderId);
            }
            else if (action == Constants.ActionModify)
            {
                ProcessUpsert(tick.OrderId, tick.Side, tick.Price);
            }
            else if (action == Constants.ActionClearY || action == Constants.ActionClearF)
            {
                ClearBook();
            }

            bestBidByTick[i] = _bestBid;
            bestAskByTick[i] = _bestAsk;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessUpsert(long orderId, byte newSide, int newPrice)
    {
        ref var stateRef = ref _activeOrders.GetValueRefOrAddDefault(orderId, out var exists);
        if (exists)
        {
            var oldState = stateRef;
            var isAsk = StatePacker.IsOptimizedAsk(oldState);
            var oldPrice = StatePacker.GetOptimizedPrice(oldState);

            var needRepairOldBid = false;
            var needRepairOldAsk = false;

            if (!isAsk)
            {
                _bidCountByPrice[oldPrice]--;
                if (_bidCountByPrice[oldPrice] == 0 && oldPrice == _bestBid)
                {
                    needRepairOldBid = true;
                }
            }
            else
            {
                _askCountByPrice[oldPrice]--;
                if (_askCountByPrice[oldPrice] == 0 && oldPrice == _bestAsk)
                {
                    needRepairOldAsk = true;
                }
            }

            stateRef = StatePacker.PackOptimizedState(newSide, newPrice);

            if (newSide == Constants.SideBid)
            {
                if (_bidCountByPrice[newPrice] == 0)
                {
                    _touchedBidPrices[_touchedBidCount++] = newPrice;
                }
                _bidCountByPrice[newPrice]++;
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
            stateRef = StatePacker.PackOptimizedState(newSide, newPrice);

            if (newSide == Constants.SideBid)
            {
                if (_bidCountByPrice[newPrice] == 0)
                {
                    _touchedBidPrices[_touchedBidCount++] = newPrice;
                }
                _bidCountByPrice[newPrice]++;
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

        var isAsk = StatePacker.IsOptimizedAsk(state);
        var price = StatePacker.GetOptimizedPrice(state);

        if (!isAsk)
        {
            _bidCountByPrice[price]--;
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
