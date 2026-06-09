using System;
using System.Collections.Generic;

namespace OrderBookTask;

internal sealed class FullOrderBookProcessor
{
    private readonly Dictionary<long, FullOrderState> _activeOrders;
    private readonly int[] _bidCountByPrice;
    private readonly int[] _bidQtyByPrice;
    private readonly int[] _askCountByPrice;
    private readonly int[] _askQtyByPrice;
    private readonly int _maxPrice;
    private int _bestBid;
    private int _bestAsk;

    public FullOrderBookProcessor(int maxPrice, int expectedOrderCapacity)
    {
        _maxPrice = maxPrice;
        _activeOrders = new Dictionary<long, FullOrderState>(expectedOrderCapacity);
        _bidCountByPrice = new int[maxPrice + 1];
        _bidQtyByPrice = new int[maxPrice + 1];
        _askCountByPrice = new int[maxPrice + 1];
        _askQtyByPrice = new int[maxPrice + 1];
        _bestBid = -1;
        _bestAsk = -1;
    }

    public void Reset()
    {
        ClearBook();
    }

    private void ClearBook()
    {
        _activeOrders.Clear();
        Array.Clear(_bidCountByPrice, 0, _bidCountByPrice.Length);
        Array.Clear(_bidQtyByPrice, 0, _bidQtyByPrice.Length);
        Array.Clear(_askCountByPrice, 0, _askCountByPrice.Length);
        Array.Clear(_askQtyByPrice, 0, _askQtyByPrice.Length);
        _bestBid = -1;
        _bestAsk = -1;
    }

    public void Process(Tick[] ticks, FullResultArrays results)
    {
        for (var i = 0; i < ticks.Length; i++)
        {
            ref readonly var tick = ref ticks[i];
            var action = tick.Action;

            if (action == Constants.ActionClearY || action == Constants.ActionClearF)
            {
                ClearBook();
            }
            else if (action == Constants.ActionAdd || action == Constants.ActionModify)
            {
                ProcessUpsert(tick.OrderId, tick.Side, tick.Price, tick.Qty);
            }
            else if (action == Constants.ActionDelete)
            {
                ProcessDelete(tick.OrderId);
            }

            if (_bestBid == -1)
            {
                results.BestBidByTick[i] = -1;
                results.BestBidQtyByTick[i] = 0;
                results.BestBidCountByTick[i] = 0;
            }
            else
            {
                results.BestBidByTick[i] = _bestBid;
                results.BestBidQtyByTick[i] = _bidQtyByPrice[_bestBid];
                results.BestBidCountByTick[i] = _bidCountByPrice[_bestBid];
            }

            if (_bestAsk == -1)
            {
                results.BestAskByTick[i] = -1;
                results.BestAskQtyByTick[i] = 0;
                results.BestAskCountByTick[i] = 0;
            }
            else
            {
                results.BestAskByTick[i] = _bestAsk;
                results.BestAskQtyByTick[i] = _askQtyByPrice[_bestAsk];
                results.BestAskCountByTick[i] = _askCountByPrice[_bestAsk];
            }
        }
    }

    private void ProcessUpsert(long orderId, byte newSide, int newPrice, int newQty)
    {
        if (_activeOrders.TryGetValue(orderId, out var oldState))
        {
            var oldSide = oldState.Side;
            var oldPrice = oldState.Price;
            var oldQty = oldState.Qty;

            var needRepairOldBid = false;
            var needRepairOldAsk = false;

            if (oldSide == Constants.SideBid)
            {
                _bidCountByPrice[oldPrice]--;
                _bidQtyByPrice[oldPrice] -= oldQty;
                if (_bidCountByPrice[oldPrice] == 0 && oldPrice == _bestBid)
                {
                    needRepairOldBid = true;
                }
            }
            else if (oldSide == Constants.SideAsk)
            {
                _askCountByPrice[oldPrice]--;
                _askQtyByPrice[oldPrice] -= oldQty;
                if (_askCountByPrice[oldPrice] == 0 && oldPrice == _bestAsk)
                {
                    needRepairOldAsk = true;
                }
            }

            _activeOrders[orderId] = new FullOrderState(newSide, newPrice, newQty);

            if (newSide == Constants.SideBid)
            {
                _bidCountByPrice[newPrice]++;
                _bidQtyByPrice[newPrice] += newQty;
                if (newPrice > _bestBid)
                {
                    _bestBid = newPrice;
                }
            }
            else if (newSide == Constants.SideAsk)
            {
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
            _activeOrders[orderId] = new FullOrderState(newSide, newPrice, newQty);

            if (newSide == Constants.SideBid)
            {
                _bidCountByPrice[newPrice]++;
                _bidQtyByPrice[newPrice] += newQty;
                if (newPrice > _bestBid)
                {
                    _bestBid = newPrice;
                }
            }
            else if (newSide == Constants.SideAsk)
            {
                _askCountByPrice[newPrice]++;
                _askQtyByPrice[newPrice] += newQty;
                if (_bestAsk == -1 || newPrice < _bestAsk)
                {
                    _bestAsk = newPrice;
                }
            }
        }
    }

    private void ProcessDelete(long orderId)
    {
        if (!_activeOrders.Remove(orderId, out var state))
        {
            return;
        }

        var side = state.Side;
        var price = state.Price;
        var qty = state.Qty;

        if (side == Constants.SideBid)
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
        else if (side == Constants.SideAsk)
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
