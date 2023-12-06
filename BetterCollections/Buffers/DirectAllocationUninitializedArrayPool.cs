using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace BetterCollections.Buffers;

/// <summary>It is safe to leak array allocations</summary>
public sealed class DirectAllocationUninitializedArrayPool<T> : ArrayPool<T>
{
    public static DirectAllocationUninitializedArrayPool<T> Instance { get; } = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override T[] Rent(int minimumLength)
    {
        if (minimumLength == 0) return Array.Empty<T>();
#if NET6_0_OR_GREATER
        return GC.AllocateUninitializedArray<T>(minimumLength);
#else
        return new T[minimumLength];
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Return(T[] array, bool clearArray = false) { }
}
