#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace BetterCollections.Cryptography.AHasherImpl;

public struct AHasher2Data
{
    public Vector128<byte> enc;
    public Vector128<byte> sum;
    public readonly Vector128<byte> key;

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public AHasher2Data(Vector128<byte> enc, Vector128<byte> sum, Vector128<byte> key)
    {
        Unsafe.SkipInit(out this);
        this.enc = enc;
        this.sum = sum;
        this.key = key;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public AHasher2Data(Vector128<byte> enc)
    {
        Unsafe.SkipInit(out this);
        this.enc = enc;
    }

    public ulong buffer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
        get => enc.AsUInt64().GetElement(0);
        [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
        set => enc.AsUInt64().WithElement(0, value);
    }

    public ulong pad
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
        get => enc.AsUInt64().GetElement(1);
        [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
        set => enc.AsUInt64().WithElement(1, value);
    }

    public Vector128<ulong> extra
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
        get => sum.AsUInt64();
        [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
        set => sum = value.AsByte();
    }

    public Vector128<ulong> buffer_extra1
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
        get => Vector128.ConditionalSelect(Vector128.Create(ulong.MaxValue, 0), enc.AsUInt64(), sum.AsUInt64());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public AHasher2Data(ulong buffer, ulong pad, Vector128<byte> extra)
    {
        Unsafe.SkipInit(out this);
        this.buffer = buffer;
        this.pad = pad;
        this.extra = extra.AsUInt64();
    }
}

#endif
