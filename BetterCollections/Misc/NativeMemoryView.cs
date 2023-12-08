using System;
using System.Runtime.CompilerServices;

namespace BetterCollections.Misc;

public readonly unsafe struct NativeMemoryView<T>(T* ptr, nuint len) where T : unmanaged
{
    public readonly T* ptr = ptr;
    public readonly nuint len = len;

    public ref T this[nuint i]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref *(ptr + i);
    }

    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => len == 0;
    }

    public Span<T> AsSpan() => new(ptr, (int)len);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeMemoryView<U> As<U>() where U : unmanaged
    {
        var num1 = (nuint)Unsafe.SizeOf<T>();
        var num2 = (nuint)Unsafe.SizeOf<U>();
        var length1 = len;
        var length2 = num1 != num2 ? num1 != 1U ? unchecked(length1 * num1 / num2) : length1 / num2 : length1;
        return new((U*)ptr, length2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeMemoryView<T> Slice(nuint start)
    {
        if ((uint)start > (uint)len) throw new ArgumentOutOfRangeException();
        return new(ptr + start, len - start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeMemoryView<T> Slice(nuint start, nuint length)
    {
        if (start + length > len) throw new ArgumentOutOfRangeException();
        return new(ptr + start, length);
    }
}
