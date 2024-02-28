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

    private const uint Empty = uint.MaxValue - 1;
    private const uint Pending = uint.MaxValue;

    private readonly ArrayPool<uint> poolUint;
    private readonly ArrayPool<Page> poolPage;
    private readonly object pagesLocker = new();
    private Page[] pages;
    private uint size;

    public UIntConcurrentPagedSparseSet() : this(ArrayPoolFactory.DirectAllocation) { }

    public UIntConcurrentPagedSparseSet(ArrayPoolFactory poolFactory)
    {
        if (!poolFactory.MustReturn) GC.SuppressFinalize(this);
        poolUint = poolFactory.Get<uint>();
        poolPage = poolFactory.Get<Page>();
        pages = poolPage.Rent(InitPages);
    }

    private struct Page
    {
        public uint[]? packed;
        public uint[]? sparse;
        public object? lockerPacked;
        public object? lockerSparse;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint[] GetPacked(UIntConcurrentPagedSparseSet self)
        {
            if (packed == null)
            {
                if (lockerPacked == null)
                {
                    Interlocked.CompareExchange(ref lockerPacked, new(), null);
                }
                while (packed == null)
                {
                    if (!Monitor.TryEnter(lockerPacked)) continue;
                    try
                    {
                        packed ??= self.poolUint.Rent(PageSize);
                    }
                    finally
                    {
                        Monitor.Exit(lockerPacked);
                    }
                }
            }
            return packed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint[] GetSparse(UIntConcurrentPagedSparseSet self)
        {
            if (sparse == null)
            {
                var locker = GetSparseLocker();
                while (sparse == null)
                {
                    if (!Monitor.TryEnter(locker)) continue;
                    try
                    {
                        sparse = self.poolUint.Rent(PageSize);
                        sparse.AsSpan(0, PageSize).Fill(Empty);
                    }
                    finally
                    {
                        Monitor.Exit(locker);
                    }
                }
            }
            return sparse;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetSparseLocker()
        {
            if (lockerSparse == null)
            {
                Interlocked.CompareExchange(ref lockerSparse, new(), null);
            }
            return lockerSparse;
        }
    }

    #region Dispose

    private void ReleaseUnmanagedResources()
    {
        var pages = Interlocked.Exchange(ref this.pages!, null);
        if (pages == null!) return;
        foreach (ref var page in pages.AsSpan())
        {
            var packed = Interlocked.Exchange(ref page.packed, null);
            if (packed != null) poolUint.Return(packed);
            var sparse = Interlocked.Exchange(ref page.sparse, null);
            if (sparse != null) poolUint.Return(sparse);
            page.lockerPacked = null;
            page.lockerSparse = null;
        }
        poolPage.Return(pages);
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~UIntConcurrentPagedSparseSet() => ReleaseUnmanagedResources();

    #endregion

    #region Private

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref Page GetPage(uint page)
    {
        if (page >= pages.Length) GrowPages(page);
        return ref pages[page];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GrowPages(uint page)
    {
        while (page >= pages.Length)
        {
            if (Monitor.TryEnter(pagesLocker))
            {
                try
                {
                    if (page < pages.Length) return;
                    var oldPages = pages;
                    var size = page.CeilPowerOf2();
                    if (size > int.MaxValue) throw new OutOfMemoryException();
                    var newPages = poolPage.Rent((int)size);
                    oldPages.AsSpan().CopyTo(newPages);
                    Interlocked.Exchange(ref pages, newPages);
                    poolPage.Return(oldPages, true);
                }
                finally
                {
                    Monitor.Exit(pagesLocker);
                }
            }
        }
    }

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
        ref var sparsePage = ref GetPage(item / PageSize);
        ref var sparseSlot = ref sparsePage.GetSparse(this)[item % PageSize];
        var current = sparseSlot;
        if (current == Pending) return;
        if (current != Interlocked.CompareExchange(ref sparseSlot, Pending, current)) return;
        var index = Interlocked.Increment(ref size) - 1;
        try
        {
            ref var packedPage = ref GetPage(index / PageSize);
            packedPage.GetPacked(this)[index % PageSize] = item;
        }
        finally
        {
            sparseSlot = index;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(uint item)
    {
        ref var sparsePage = ref GetPage(item / PageSize);
        return sparsePage.GetSparse(this)[item % PageSize] != Empty;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(uint item)
    {
        ref var sparsePage = ref GetPage(item / PageSize);
        ref var indexSlot = ref sparsePage.GetSparse(this)[item % PageSize];
        var index = indexSlot;
        if (index == Empty) return false;
        lock (sparsePage.GetSparseLocker())
        {
            indexSlot = Empty;
            var endIndex = Interlocked.Decrement(ref size);
            if (endIndex != 0)
            {
                ref var packedPage = ref GetPage(index / PageSize);
                ref var packedEndPage = ref GetPage(endIndex / PageSize);
                ref var slot = ref packedPage.GetPacked(this)[index % PageSize];
                var end = packedEndPage.GetPacked(this)[endIndex % PageSize];
                slot = end;
                ref var sparseEndPage = ref GetPage(end / PageSize);
                sparseEndPage.GetSparse(this)[end % PageSize] = index;
            }
            return true;
        }
    }

    /// <summary>
    /// <see cref="Clear"/> is not thread safe
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        size = 0;
        foreach (var page in pages)
        {
            page.sparse?.AsSpan().Fill(Empty);
        }
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
    public IEnumerator<uint> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion
}

#endif
