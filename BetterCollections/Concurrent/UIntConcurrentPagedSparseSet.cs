#if !NETSTANDARD
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using BetterCollections.Buffers;
using BetterCollections.Misc;

namespace BetterCollections.Concurrent;

public sealed class UIntConcurrentPagedSparseSet : IDisposable, ICollection<uint>
{
    private const int PageSize = 256;
    private const int InitPages = 4;

    private const uint Empty = uint.MaxValue;

    private Paged sparse;
    private Paged packed;
    private uint size;

    private sealed class Pools(ArrayPool<uint> u, ArrayPool<Page> page)
    {
        public readonly ArrayPool<uint> uInt = u;
        public readonly ArrayPool<Page> page = page;
    }

    public UIntConcurrentPagedSparseSet() : this(ArrayPoolFactory.DirectAllocation) { }

    public UIntConcurrentPagedSparseSet(ArrayPoolFactory poolFactory)
    {
        if (!poolFactory.MustReturn) GC.SuppressFinalize(this);
        var pools = new Pools(poolFactory.Get<uint>(), poolFactory.Get<Page>());
        sparse = new(pools, InitPages, true);
        packed = new(pools, InitPages, false);
    }

    private struct Paged
    {
        private readonly Pools pools;
        private Page[] pages;
        private readonly object locker = new();
        private readonly bool fill;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Paged(Pools pools, int initPages, bool fill)
        {
            this.pools = pools;
            this.fill = fill;
            this.pages = pools.page.Rent(initPages);
            var pages = this.pages;
            for (var i = 0; i < pages.Length; i++)
            {
                pages[i] = new(pools, fill);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release()
        {
            foreach (var page in pages)
            {
                page.Release();
            }
            pools.page.Return(pages, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Page Get(uint index)
        {
            var p = index / PageSize;
            if (p >= pages.Length) Grow(p);
            return pages[p];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref uint Slot(uint index) => ref Get(index).Slot(index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Grow(uint p)
        {
            while (p >= pages.Length)
            {
                if (Monitor.TryEnter(locker))
                {
                    try
                    {
                        if (p < pages.Length) return;
                        var oldPages = pages;
                        var size = Math.Max(p + 4, (uint)pages.Length * 2).CeilPowerOf2();
                        if (size > int.MaxValue) throw new OutOfMemoryException();
                        var newPages = pools.page.Rent((int)size);
                        oldPages.AsSpan().CopyTo(newPages);
                        for (var i = oldPages.Length; i < newPages.Length; i++)
                        {
                            newPages[i] = new(pools, fill);
                        }
                        Interlocked.Exchange(ref pages, newPages);
                        pools.page.Return(oldPages, true);
                        return;
                    }
                    finally
                    {
                        Monitor.Exit(locker);
                    }
                }
            }
        }
    }

    private sealed class Page(Pools pools, bool fill)
    {
        private uint[]? data;
        private readonly object locker = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref uint Slot(uint index)
        {
            if (data == null) Create();
            return ref data![index % PageSize];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Create()
        {
            while (data == null)
            {
                if (Monitor.TryEnter(locker))
                {
                    try
                    {
                        if (data != null) return;
                        var arr = pools.uInt.Rent(PageSize);
                        if (fill) arr.AsSpan().Fill(Empty);
                        data = arr;
                        return;
                    }
                    finally
                    {
                        Monitor.Exit(locker);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release()
        {
            if (data == null) return;
            pools.uInt.Return(data);
        }
    }

    #region Dispose

    private void ReleaseUnmanagedResources()
    {
        sparse.Release();
        packed.Release();
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~UIntConcurrentPagedSparseSet() => ReleaseUnmanagedResources();

    #endregion

    #region ICollection

    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (int)size;
    }
    public bool IsReadOnly
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(uint item)
    {
        var index = Interlocked.Increment(ref size) - 1;
        packed.Slot(index) = item;
        sparse.Slot(item) = index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(uint item) => sparse.Slot(item) != Empty;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(uint item)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// <see cref="Clear"/> is not thread safe
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// <see cref="CopyTo"/> is not thread safe
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(uint[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator<uint> IEnumerable<uint>.GetEnumerator() => GetEnumerator();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator(UIntConcurrentPagedSparseSet self) : IEnumerator<uint>
    {
        private int i = -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (i < self.size - 1)
            {
                i++;
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            i = -1;
        }

        public uint Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => self.packed.Slot((uint)i);
        }
        object IEnumerator.Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Current;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() { }
    }

    #endregion
}

#endif
