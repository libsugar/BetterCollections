using System;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if NET7_0_OR_GREATER
using System.Runtime.Intrinsics;
using X86 = System.Runtime.Intrinsics.X86;
using Arm = System.Runtime.Intrinsics.Arm;
#endif

namespace BetterCollections.Cryptography;

// reference https://github.com/tkaitchuck/aHash/tree/master

/// <summary>
/// A hasher that ensures even distribution of each bit
/// <para>If possible use Aes SIMD acceleration (.net7+)</para>
/// </summary>
public readonly struct AHasher : IHasher
{
#if NET7_0_OR_GREATER
    private readonly Union union;
    private readonly bool soft;

    [StructLayout(LayoutKind.Explicit)]
    private readonly struct Union
    {
        [FieldOffset(0)]
        public readonly AesHasher aesHasher;
        [FieldOffset(0)]
        public readonly SoftHasher softHasher;

        public Union(AesHasher aesHasher)
        {
            this.aesHasher = aesHasher;
        }

        public Union(SoftHasher softHasher)
        {
            this.softHasher = softHasher;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AHasher()
    {
        var rand = Random.Shared;
        Span<Vector128<byte>> bytes = stackalloc Vector128<byte>[2];
        rand.NextBytes(MemoryMarshal.Cast<Vector128<byte>, byte>(bytes));
        if (X86.Aes.IsSupported || Arm.Aes.IsSupported)
        {
            union = new(new AesHasher(bytes[0], bytes[1]));
            soft = false;
        }
        else
        {
            union = new(new SoftHasher(MemoryMarshal.Cast<Vector128<byte>, ulong>(bytes)));
            soft = true;
        }
    }

    [ThreadStatic]
    private static AHasher _thread_current;
    [ThreadStatic]
    private static bool _thread_current_has;

    public static AHasher ThreadCurrent
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (!_thread_current_has)
            {
                _thread_current = new();
                _thread_current_has = true;
            }
            return _thread_current;
        }
    }

    public AHasher(ReadOnlySpan<ulong> keys)
    {
        if (keys.Length < 4) throw new ArgumentOutOfRangeException(nameof(keys), "length of keys must >= 4");
        if (X86.Aes.IsSupported || Arm.Aes.IsSupported)
        {
            var bytes = MemoryMarshal.Cast<ulong, Vector128<byte>>(keys);
            union = new(new AesHasher(bytes[0], bytes[1]));
            soft = false;
        }
        else
        {
            union = new(new SoftHasher(keys));
        }
    }

    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly struct AesHasher(Vector128<byte> key1, Vector128<byte> key2)
    {
        private readonly Vector128<byte> enc = key1;
        private readonly Vector128<byte> sum = key2;
        private readonly Vector128<byte> key = key1 ^ key2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Hash(ulong value)
        {
            var target = Vector128.Create(value, 0).AsByte();
            var enc = AesDec(this.enc, target);
            var sum = (this.sum.AsUInt64() + target.AsUInt64()).AsByte();
            var combined = AesEnc(sum, enc);
            var result = AesDec(combined, key);
            result = AesDec(result, result);
            return result.AsUInt64().GetElement(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<byte> AesDec(Vector128<byte> value, Vector128<byte> xor)
        {
            if (X86.Aes.IsSupported)
            {
                return X86.Aes.Decrypt(value, xor);
            }
            else if (Arm.Aes.IsSupported)
            {
                var a = Arm.Aes.Decrypt(value, Vector128<byte>.Zero);
                a = Arm.Aes.InverseMixColumns(a);
                return xor ^ a;
            }
            else throw new PlatformNotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<byte> AesEnc(Vector128<byte> value, Vector128<byte> xor)
        {
            if (X86.Aes.IsSupported)
            {
                return X86.Aes.Encrypt(value, xor);
            }
            else if (Arm.Aes.IsSupported)
            {
                var a = Arm.Aes.Encrypt(value, Vector128<byte>.Zero);
                a = Arm.Aes.MixColumns(a);
                return xor ^ a;
            }
            else throw new PlatformNotSupportedException();
        }
    }
#else
    private readonly SoftHasher softHasher;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AHasher()
    {
#if NETSTANDARD
        var rand = new Random();
#else
        var rand = Random.Shared;
#endif
        Span<ulong> keys = stackalloc ulong[2];
        rand.NextBytes(MemoryMarshal.Cast<ulong, byte>(keys));
        softHasher = new(keys);
    }

    public AHasher(ReadOnlySpan<ulong> keys)
    {
        if (keys.Length < 4) throw new ArgumentOutOfRangeException(nameof(keys), "length of keys must >= 4");
        softHasher = new(keys);
    }
#endif


    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly struct SoftHasher(ReadOnlySpan<ulong> keys)
    {
        public readonly ulong buffer = keys[0];
        public readonly ulong pad = keys[1];

        /// <summary>
        /// This constant comes from Kunth's prng (Empirically it works better than those from splitmix32)
        /// </summary>
        private const ulong MULTIPLE = 6364136223846793005;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Hash(ulong value)
        {
            var buffer = FoldedMultiply(value ^ this.buffer, MULTIPLE);
            var rot = (int)(buffer & 63);
#if NETSTANDARD
            return RotateLeft(FoldedMultiply(buffer, pad), rot);
#else
            return BitOperations.RotateLeft(FoldedMultiply(buffer, pad), rot);
#endif
        }

#if NETSTANDARD
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong RotateLeft(ulong value, int offset)
            => (value << offset) | (value >> (64 - offset));
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong FoldedMultiply(ulong s, ulong by)
        {
#if NET7_0_OR_GREATER
            var result = unchecked((UInt128)s * by);
            return (ulong)(result & 0xffff_ffff_ffff_ffff) ^ (ulong)(result >> 64);
#else
            var b1 = unchecked(s * BinaryPrimitives.ReverseEndianness(by));
            var b2 = unchecked(BinaryPrimitives.ReverseEndianness(s) * ~by);
            return b1 ^ BinaryPrimitives.ReverseEndianness(b2);
#endif
        }
    }

    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Hash(ulong value)
    {
#if NET7_0_OR_GREATER
        if (!soft) return union.aesHasher.Hash(value);
        return union.softHasher.Hash(value);
#else
        return softHasher.Hash(value);
#endif
    }
}
