using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if NET7_0_OR_GREATER
namespace BetterCollections.Misc;

public sealed unsafe class AlignedMemoryHandle<T> : IDisposable
    where T : unmanaged
{
    public T* ptr;
    public nuint len;

    public ref T this[nuint i]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref *(ptr + i);
    }

    public NativeMemoryView<T> AsView() => new(ptr, len);

    public Span<T> AsSpan() => new(ptr, (int)len);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Alloc(nuint size) => AllocUnsafe(size * (nuint)Unsafe.SizeOf<T>(), (nuint)Utils.AlignmentOf<T>());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReAlloc(nuint size) => ReAllocUnsafe(size * (nuint)Unsafe.SizeOf<T>(), (nuint)Utils.AlignmentOf<T>());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AllocUnsafe(nuint size, nuint align)
    {
        if (ptr != null) ReAllocUnsafe(size, align);
        ptr = (T*)NativeMemory.AlignedAlloc(size, align);
        len = size / (nuint)Unsafe.SizeOf<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReAllocUnsafe(nuint size, nuint align)
    {
        if (ptr == null) AllocUnsafe(size, align);
        else
        {
            ptr = (T*)NativeMemory.AlignedRealloc(ptr, size, align);
            len = size / (nuint)Unsafe.SizeOf<T>();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Free()
    {
        if (ptr != null)
        {
            NativeMemory.AlignedFree(ptr);
            ptr = null;
            len = 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        Free();
        GC.SuppressFinalize(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    ~AlignedMemoryHandle() => Free();
}

#endif
