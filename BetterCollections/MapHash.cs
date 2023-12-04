using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BetterCollections.Buffers;
using BetterCollections.Misc;

namespace BetterCollections;

public class MapHash<TKey, TValue> : ASwissTable<(TKey Key, TValue Value), MapEH<TKey, TValue>>,
    IDictionary<TKey, TValue>
{
    public MapHash(ArrayPoolFactory poolFactory, int cap, IEqualityComparer<TKey>? comparer) : base(poolFactory,
        new MapEH<TKey, TValue>(comparer), cap) { }

    // ReSharper disable once IntroduceOptionalParameters.Global
    public MapHash(ArrayPoolFactory poolFactory, int cap) : this(poolFactory, cap, null) { }

    // ReSharper disable once IntroduceOptionalParameters.Global
    public MapHash(ArrayPoolFactory poolFactory) : this(poolFactory, 0, null) { }

    public MapHash(int cap, IEqualityComparer<TKey>? comparer) :
        this(ArrayPoolFactory.DirectAllocation, cap, comparer) { }

    // ReSharper disable once IntroduceOptionalParameters.Global
    public MapHash(int cap) : this(cap, null) { }

    public MapHash() : this(0, null) { }

    public bool IsReadOnly => false;

    public TValue this[TKey key]
    {
        get => throw new System.NotImplementedException();
        set => throw new System.NotImplementedException();
    }

    public void Add((TKey key, TValue value) item)
    {
        TryInsert(item, InsertBehavior.None); // todo throw
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(TKey key, TValue value) => Add((key, value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(KeyValuePair<TKey, TValue> item) => Add((item.Key, item.Value));

    public bool ContainsKey(TKey key)
    {
        throw new System.NotImplementedException();
    }

    public bool Remove(TKey key)
    {
        throw new System.NotImplementedException();
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        throw new System.NotImplementedException();
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        throw new System.NotImplementedException();
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        throw new System.NotImplementedException();
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        throw new System.NotImplementedException();
    }

    public ICollection<TKey> Keys => throw new System.NotImplementedException();

    public ICollection<TValue> Values => throw new System.NotImplementedException();

    public ICollection<(TKey Key, TValue Value)> Items => throw new System.NotImplementedException();

    public ICollection<KeyValuePair<TKey, TValue>> Pairs => throw new System.NotImplementedException();

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        throw new System.NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
