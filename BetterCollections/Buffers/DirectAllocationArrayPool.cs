using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace BetterCollections.Buffers;

/// <summary>It is safe to leak array allocations</summary>
public sealed class DirectAllocationArrayPool<T> : ArrayPool<T>
{
    public static DirectAllocationArrayPool<T> Instance { get; } = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override T[] Rent(int minimumLength)
    {
        if (minimumLength == 0) return Array.Empty<T>();
        return new T[minimumLength];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Return(T[] array, bool clearArray = false) { }
}
