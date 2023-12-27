using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#if NET7_0_OR_GREATER
using System.Runtime.Intrinsics;
#endif

namespace BetterCollections.Cryptography.AHasherImpl;

public struct AHasher2Data
{
#if NET7_0_OR_GREATER
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
    
    [UnscopedRef]
    public ref ulong buffer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
        get => ref Unsafe.As<AHasher2Data, ulong>(ref this);
    }

    [UnscopedRef]
    public ref ulong pad
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
        get => ref Unsafe.Add(ref Unsafe.As<AHasher2Data, ulong>(ref this), 1);
    }
#else
    public ulong buffer;
    public ulong pad;
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public AHasher2Data(ulong buffer, ulong pad)
    {
        Unsafe.SkipInit(out this);
        this.buffer = buffer;
        this.pad = pad;
    }
}
