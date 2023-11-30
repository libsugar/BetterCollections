using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using BetterCollections.Buffers;
using BetterCollections.Misc;

namespace BetterCollections;

public class MapHash<TKey, TValue> : AHashTable<TKey>
{
    protected readonly ArrayPool<TValue> poolValue;

    private TValue[]? values;

    #region Ctor

    public MapHash() : this(0) { }

    // ReSharper disable once IntroduceOptionalParameters.Global
    public MapHash(int cap) : this(cap, (IEqualityComparer<TKey>?)null) { }

    public MapHash(int cap, IEqualityComparer<TKey>? comparer) :
        this(cap, comparer, ArrayPoolFactory.DirectAllocation) { }

    public MapHash(ArrayPoolFactory poolFactory) : this(0, poolFactory) { }
    public MapHash(int cap, ArrayPoolFactory poolFactory) : this(cap, null, poolFactory) { }

    public MapHash(int cap, IEqualityComparer<TKey>? comparer, ArrayPoolFactory poolFactory) : base(cap, comparer,
        poolFactory)
    {
        poolValue = poolFactory.Get<TValue>();
    }

    #endregion

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (values != null)
        {
            poolValue.Return(values, RuntimeHelpers.IsReferenceOrContainsReferences<TValue>());
            values = null;
        }
    }

    #endregion

    protected override void RentDataArray(int len)
    {
        var values = poolValue.Rent(len);
        this.values = values;
    }

    protected override void ReRentDataArray(int oldLen, int newLen)
    {
        var oldValues = this.values!;
        var values = poolValue.Rent(newLen);
        oldValues.AsSpan(0, oldLen).CopyTo(values.AsSpan(0, oldLen));
        poolValue.Return(oldValues, RuntimeHelpers.IsReferenceOrContainsReferences<TValue>());
        this.values = values;
    }

    public void Add(TKey key, TValue value)
    {
        // ReSharper disable once RedundantAssignment
        var r = TryInsert(key, value, InsertBehavior.OverwriteIfExisting);
        Debug.Assert(r);
    }

    public bool TryAdd(TKey key, TValue value) =>
        TryInsert(key, value, InsertBehavior.None);

    protected bool TryInsert(TKey key, TValue value, InsertBehavior behavior) =>
        base.TryInsert(key, new ExtraData(value), behavior);

    private readonly struct ExtraData(TValue value) : IExtraData
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetData(AHashTable<TKey> baseThis, int index)
        {
            var self = (MapHash<TKey, TValue>)baseThis;
            self.values![index] = value;
        }
    }
}
