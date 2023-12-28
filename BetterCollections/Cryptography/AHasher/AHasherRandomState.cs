#if NET8_0_OR_GREATER
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace BetterCollections.Cryptography;

public readonly struct AHasherRandomState
{
    public readonly Vector128<byte> key1;
    public readonly Vector128<byte> key2;

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public AHasherRandomState(Vector128<byte> key1, Vector128<byte> key2)
    {
        Unsafe.SkipInit(out this);
        this.key1 = key1;
        this.key2 = key2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public AHasherRandomState(ReadOnlySpan<ulong> keys)
    {
        if (keys.Length < 4) throw new ArgumentOutOfRangeException(nameof(keys), "length of keys must >= 4");
        Unsafe.SkipInit(out this);
        key1 = Vector128.LoadUnsafe(in keys[0]).AsByte();
        key2 = Vector128.LoadUnsafe(in keys[2]).AsByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public AHasherRandomState(ulong a, ulong b)
    {
        Unsafe.SkipInit(out this);
        Unsafe.AsRef(in this.a) = a;
        Unsafe.AsRef(in this.b) = b;
    }

    #region Getters

    [UnscopedRef]
    public ref readonly ulong a
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
        get => ref Unsafe.As<AHasherRandomState, ulong>(ref Unsafe.AsRef(in this));
    }

    [UnscopedRef]
    public ref readonly ulong b
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
        get => ref Unsafe.Add(ref Unsafe.As<AHasherRandomState, ulong>(ref Unsafe.AsRef(in this)), 1);
    }

    #endregion

    [UnscopedRef]
    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public ReadOnlySpan<ulong> AsSpan() => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in a), 4);

    [UnscopedRef]
    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public Span<ulong> UnsafeAsMutableSpan() => MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in a), 4);
}

#endif
