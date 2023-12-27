using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if NET6_0_OR_GREATER
using System.Runtime.Intrinsics;
#endif

namespace BetterCollections.Cryptography;

#if NET8_0_OR_GREATER
[InlineArray(4)]
[StructLayout(LayoutKind.Sequential, Size = sizeof(ulong) * 4)]
public struct AHasherKeys
{
    public ulong key;
    
    [UnscopedRef]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<ulong> AsSpan() => MemoryMarshal.CreateSpan(ref key, 4);
}
#endif

[StructLayout(LayoutKind.Explicit)]
public struct AHasherRandomState
{
    [FieldOffset(0)]
    public ulong a;
    [FieldOffset(sizeof(ulong))]
    public ulong b;
    [FieldOffset(sizeof(ulong) * 2)]
    public ulong c;
    [FieldOffset(sizeof(ulong) * 3)]
    public ulong d;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AHasherRandomState(ulong a, ulong b, ulong c, ulong d)
    {
        Unsafe.SkipInit(out this);
        this.a = a;
        this.b = b;
        this.c = c;
        this.d = d;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AHasherRandomState(ReadOnlySpan<ulong> keys)
    {
        if (keys.Length < 4) throw new ArgumentOutOfRangeException(nameof(keys), "length of keys must >= 4");
        Unsafe.SkipInit(out this);
        a = keys[0];
        b = keys[1];
        c = keys[2];
        d = keys[3];
    }

#if NET8_0_OR_GREATER
    [FieldOffset(0)]
    public AHasherKeys keys;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AHasherRandomState(AHasherKeys keys)
    {
        Unsafe.SkipInit(out this);
        this.keys = keys;
    }
#endif

#if NET6_0_OR_GREATER
    [FieldOffset(0)]
    public Vector128<byte> key1;
    [FieldOffset(sizeof(ulong) * 2)]
    public Vector128<byte> key2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AHasherRandomState(Vector128<byte> key1, Vector128<byte> key2)
    {
        Unsafe.SkipInit(out this);
        this.key1 = key1;
        this.key2 = key2;
    }
#endif

    [UnscopedRef]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<ulong> AsSpan() => MemoryMarshal.CreateSpan(ref a, 4);
}
