using System;
using System.Runtime.CompilerServices;

namespace OrderBookTask;

internal sealed class LongStateMap<TState> where TState : struct
{
    private readonly long[] _keys;
    private readonly TState[] _values;
    private readonly int[] _slotEpochs;
    private readonly int _mask;
    private int _epoch;
    private int _count;

    public LongStateMap(int expectedCapacity)
    {
        // Use at least next power of two greater than expectedCapacity * 2.
        int target = expectedCapacity * 2;
        int capacity = 8;
        while (capacity <= target)
        {
            capacity <<= 1;
        }

        _keys = new long[capacity];
        _values = new TState[capacity];
        _slotEpochs = new int[capacity];
        _mask = capacity - 1;
        _epoch = 1;
        _count = 0;
    }

    public int Count => _count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Hash(long key)
    {
        ulong x = (ulong)key;
        return (int)(x ^ (x >> 32));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        _count = 0;
        _epoch++;
        if (_epoch == int.MaxValue)
        {
            Array.Clear(_slotEpochs);
            _epoch = 1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TState GetValueRefOrAddDefault(long key, out bool exists)
    {
        int hash = Hash(key);
        int bucket = hash & _mask;
        int tombstoneIndex = -1;

        for (int i = 0; i < _keys.Length; i++)
        {
            int idx = (bucket + i) & _mask;
            int slotEpoch = _slotEpochs[idx];

            if (slotEpoch == _epoch)
            {
                if (_keys[idx] == key)
                {
                    exists = true;
                    return ref _values[idx];
                }
            }
            else if (slotEpoch == -_epoch)
            {
                if (tombstoneIndex == -1)
                {
                    tombstoneIndex = idx;
                }
            }
            else
            {
                // Empty slot for current epoch. Probing stops.
                exists = false;
                int insertIdx = tombstoneIndex != -1 ? tombstoneIndex : idx;

                if (_count >= _keys.Length)
                {
                    throw new InvalidOperationException("Map capacity exceeded");
                }

                _keys[insertIdx] = key;
                _slotEpochs[insertIdx] = _epoch;
                _count++;
                
                _values[insertIdx] = default;
                return ref _values[insertIdx];
            }
        }

        if (tombstoneIndex != -1)
        {
            exists = false;
            _keys[tombstoneIndex] = key;
            _slotEpochs[tombstoneIndex] = _epoch;
            _count++;
            
            _values[tombstoneIndex] = default;
            return ref _values[tombstoneIndex];
        }

        throw new InvalidOperationException("Map capacity exceeded");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(long key, out TState value)
    {
        int hash = Hash(key);
        int bucket = hash & _mask;

        for (int i = 0; i < _keys.Length; i++)
        {
            int idx = (bucket + i) & _mask;
            int slotEpoch = _slotEpochs[idx];

            if (slotEpoch == _epoch)
            {
                if (_keys[idx] == key)
                {
                    value = _values[idx];
                    _slotEpochs[idx] = -_epoch; // Mark as tombstone
                    _count--;
                    return true;
                }
            }
            else if (slotEpoch == -_epoch)
            {
                // Tombstone: keep probing
            }
            else
            {
                // Empty slot: stop probing
                break;
            }
        }

        value = default;
        return false;
    }
}
