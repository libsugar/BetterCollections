using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using BetterCollections.Buffers;
using BetterCollections.Cryptography;
using BetterCollections.Exceptions;
using BetterCollections.Misc;

namespace BetterCollections;

public class FlatHashMap<TKey, TValue> : FlatHashMap<TKey, TValue, AHasher>
{
    public FlatHashMap(ArrayPoolFactory poolFactory, int cap, IEqualityComparer<TKey>? comparer)
        : base(new AHasher(), poolFactory, cap, comparer) { }

    // ReSharper disable once IntroduceOptionalParameters.Global
    public FlatHashMap(ArrayPoolFactory poolFactory, int cap) : base(new AHasher(), poolFactory, cap) { }

    // ReSharper disable once IntroduceOptionalParameters.Global
    public FlatHashMap(ArrayPoolFactory poolFactory) : base(new AHasher(), poolFactory) { }

    public FlatHashMap(int cap, IEqualityComparer<TKey>? comparer) :
        base(new AHasher(), cap, comparer) { }

    // ReSharper disable once IntroduceOptionalParameters.Global
    public FlatHashMap(int cap) : base(new AHasher(), cap) { }

    public FlatHashMap() : base(new AHasher()) { }
}

public class FlatHashMap<TKey, TValue, H> : ASwissTable<(TKey Key, TValue Value), EqHash<TKey, TValue>, H>,
    IDictionary<TKey, TValue> where H : IHasher
{
    public FlatHashMap(H hasher, ArrayPoolFactory poolFactory, int cap, IEqualityComparer<TKey>? comparer) : base(
        poolFactory,
        new EqHash<TKey, TValue>(comparer), hasher, cap) { }

    // ReSharper disable once IntroduceOptionalParameters.Global
    public FlatHashMap(H hasher, ArrayPoolFactory poolFactory, int cap) : this(hasher, poolFactory, cap, null) { }

    // ReSharper disable once IntroduceOptionalParameters.Global
    public FlatHashMap(H hasher, ArrayPoolFactory poolFactory) : this(hasher, poolFactory, 0, null) { }

    public FlatHashMap(H hasher, int cap, IEqualityComparer<TKey>? comparer) :
        this(hasher, ArrayPoolFactory.DirectAllocation, cap, comparer) { }

    // ReSharper disable once IntroduceOptionalParameters.Global
    public FlatHashMap(H hasher, int cap) : this(hasher, cap, null) { }

    public FlatHashMap(H hasher) : this(hasher, 0, null) { }

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
            // ReSharper disable once RedundantAssignment
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(TKey key) => TryRemove(in key, new EqHashKey<TKey, TValue>(), out _);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        foreach (var item in this)
        {
            array[arrayIndex] = item;
            arrayIndex++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove((TKey Key, TValue Value) item) => Remove(item.Key);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);

    public ICollection<TKey> Keys => keysCollection ??= new(this);
    private KeysCollection? keysCollection;

    public ICollection<TValue> Values => valuesCollection ??= new(this);
    private ValuesCollection? valuesCollection;

    public ICollection<(TKey Key, TValue Value)> Items => tupleCollection ??= new TupleCollection(this);
    private TupleCollection? tupleCollection;


    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        => new ItemEnumerator<KeyValuePair<TKey, TValue>, KeyValueGetter<TKey, TValue>>(
            this, new(), MakeIndicesEnumerator());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    protected sealed class ItemEnumerator<T, G>(
        FlatHashMap<TKey, TValue, H> self,
        G getter,
        FullBucketsIndicesMemoryEnumerator indices)
        : IEnumerator<T>
        where G : IGetter<T, (TKey Key, TValue Value)>
    {
        private readonly int version = self.Version;

        object IEnumerator.Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Current!;
        }

        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (version != self.Version) throw new UnexpectedConcurrentException();
                return getter.Get(in self.Slots[(int)indices.Current]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => indices.MoveNext();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => indices.Reset();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => indices.Dispose();
    }

    private sealed class TupleCollection(FlatHashMap<TKey, TValue, H> self) : ICollection<(TKey Key, TValue Value)>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<(TKey Key, TValue Value)> GetEnumerator()
            => new ItemEnumerator<(TKey Key, TValue Value), IdentityGetter<(TKey Key, TValue Value)>>(
                self, new(), self.MakeIndicesEnumerator());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add((TKey Key, TValue Value) item) => self.Add(item);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => self.Clear();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains((TKey Key, TValue Value) item) => self.Contains(item);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo((TKey Key, TValue Value)[] array, int arrayIndex)
        {
            foreach (var item in this)
            {
                array[arrayIndex] = item;
                arrayIndex++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove((TKey Key, TValue Value) item) => self.Remove(item);

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => self.Count;
        }
        public bool IsReadOnly
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => self.IsReadOnly;
        }
    }

    private sealed class KeysCollection(FlatHashMap<TKey, TValue, H> self) : ICollection<TKey>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<TKey> GetEnumerator()
            => new ItemEnumerator<TKey, KeyGetter<TKey, TValue>>(
                self, new(), self.MakeIndicesEnumerator());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(TKey item) => throw new NotSupportedException();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => self.Clear();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(TKey item) => self.ContainsKey(item);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(TKey[] array, int arrayIndex)
        {
            foreach (var key in this)
            {
                array[arrayIndex] = key;
                arrayIndex++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(TKey item) => self.Remove(item);

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => self.Count;
        }
        public bool IsReadOnly
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => true;
        }
    }

    private sealed class ValuesCollection(FlatHashMap<TKey, TValue, H> self) : ICollection<TValue>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<TValue> GetEnumerator()
            => new ItemEnumerator<TValue, ValueGetter<TKey, TValue>>(
                self, new(), self.MakeIndicesEnumerator());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(TValue item) => throw new NotSupportedException();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => self.Clear();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(TValue item)
        {
            foreach (var value in this)
            {
                if (EqualityComparer<TValue>.Default.Equals(item, value)) return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(TValue[] array, int arrayIndex)
        {
            foreach (var value in this)
            {
                array[arrayIndex] = value;
                arrayIndex++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(TValue item) => throw new NotSupportedException();

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => self.Count;
        }
        public bool IsReadOnly
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => true;
        }
    }
}
