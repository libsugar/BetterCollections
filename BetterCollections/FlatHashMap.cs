using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using BetterCollections.Buffers;
using BetterCollections.Exceptions;
using BetterCollections.Misc;

namespace BetterCollections;

public class FlatHashMap<TKey, TValue> : ASwissTable<(TKey Key, TValue Value), EqHash<TKey, TValue>>,
    IDictionary<TKey, TValue>, ICollection<(TKey Key, TValue Value)>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FlatHashMap(ArrayPoolFactory poolFactory, int cap, IEqualityComparer<TKey>? comparer) : base(poolFactory,
        new EqHash<TKey, TValue>(comparer), cap) { }

    // ReSharper disable once IntroduceOptionalParameters.Global
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FlatHashMap(ArrayPoolFactory poolFactory, int cap) : this(poolFactory, cap, null) { }

    // ReSharper disable once IntroduceOptionalParameters.Global
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FlatHashMap(ArrayPoolFactory poolFactory) : this(poolFactory, 0, null) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FlatHashMap(int cap, IEqualityComparer<TKey>? comparer) :
        this(ArrayPoolFactory.DirectAllocation, cap, comparer) { }

    // ReSharper disable once IntroduceOptionalParameters.Global
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FlatHashMap(int cap) : this(cap, null) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FlatHashMap() : this(0, null) { }

    public bool IsReadOnly => false;

    public TValue this[TKey key]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (!TryGetValue(key, out var value)) throw new KeyNotFoundException();
            return value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            var r = TryInsert((key, value), InsertBehavior.OverwriteIfExisting);
            Debug.Assert(r);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add((TKey Key, TValue Value) item)
    {
        var r = TryInsert(item, InsertBehavior.FailureIfExisting);
        if (!r) throw new DuplicateKeyException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(TKey key, TValue value) => Add((key, value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(KeyValuePair<TKey, TValue> item) => Add((item.Key, item.Value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAdd((TKey key, TValue value) item) => TryInsert(item, InsertBehavior.FailureIfExisting);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAdd(TKey key, TValue value) => TryAdd((key, value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAdd(KeyValuePair<TKey, TValue> item) => TryAdd((item.Key, item.Value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(TKey key)
        => TryFind(key, new EqHashKey<TKey, TValue>(), out _);

    public bool Remove(TKey key)
    {
        throw new System.NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(TKey key, out TValue value)
    {
        if (!TryFind(key, new EqHashKey<TKey, TValue>(), out var slot_index))
        {
            value = default!;
            return false;
        }
        value = Slots[(int)slot_index].Value;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains((TKey Key, TValue Value) item) => ContainsKey(item.Key);

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        throw new System.NotImplementedException();
    }

    public void CopyTo((TKey Key, TValue Value)[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public bool Remove((TKey Key, TValue Value) item)
    {
        throw new NotImplementedException();
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        throw new System.NotImplementedException();
    }

    public ICollection<TKey> Keys => throw new System.NotImplementedException();

    public ICollection<TValue> Values => throw new System.NotImplementedException();

    public ICollection<(TKey Key, TValue Value)> Items => throw new System.NotImplementedException();

    public ICollection<KeyValuePair<TKey, TValue>> Pairs => throw new System.NotImplementedException();

    IEnumerator<(TKey Key, TValue Value)> IEnumerable<(TKey Key, TValue Value)>.GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        throw new System.NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
