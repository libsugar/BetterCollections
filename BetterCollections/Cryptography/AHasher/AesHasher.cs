#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using X86 = System.Runtime.Intrinsics.X86;
using Arm = System.Runtime.Intrinsics.Arm;

namespace BetterCollections.Cryptography.AHasherImpl;

public static class AesHasher
{
    public static bool IsSupported
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
        get => X86.Aes.IsSupported || Arm.Aes.IsSupported;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasher2Data Create(AHasherRandomState randomState) =>
        new(randomState.key1, randomState.key2, randomState.key1 ^ randomState.key2);

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasher2Data Add(AHasher2Data data, bool value) =>
        Add(data, Vector128.CreateScalar(value ? (byte)1 : (byte)0));

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasher2Data Add(AHasher2Data data, short value) =>
        Add(data, Vector128.CreateScalar(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasher2Data Add(AHasher2Data data, ushort value) =>
        Add(data, Vector128.CreateScalar(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasher2Data Add(AHasher2Data data, int value) =>
        Add(data, Vector128.CreateScalar(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasher2Data Add(AHasher2Data data, uint value) =>
        Add(data, Vector128.CreateScalar(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasher2Data Add(AHasher2Data data, long value) =>
        Add(data, Vector128.CreateScalar(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasher2Data Add(AHasher2Data data, ulong value) =>
        Add(data, Vector128.CreateScalar(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasher2Data Add(AHasher2Data data, nint value) =>
        Add(data, Vector128.CreateScalar(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasher2Data Add(AHasher2Data data, nuint value) =>
        Add(data, Vector128.CreateScalar(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasher2Data Add(AHasher2Data data, float value) =>
        Add(data, Vector128.CreateScalar(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasher2Data Add(AHasher2Data data, double value) =>
        Add(data, Vector128.CreateScalar(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasher2Data Add(AHasher2Data data, Int128 value) =>
        Add(data, Unsafe.As<Int128, Vector128<byte>>(ref value));

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasher2Data Add(AHasher2Data data, UInt128 value) =>
        Add(data, Unsafe.As<UInt128, Vector128<byte>>(ref value));

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasher2Data Add(AHasher2Data data, decimal value) =>
        Add(data, Unsafe.As<decimal, Vector128<byte>>(ref value));

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization), SkipLocalsInit]
    public static AHasher2Data Add<T>(AHasher2Data data, T value)
    {
        if (Vector128<T>.IsSupported)
            return Add(data, Vector128.CreateScalar(value).AsByte());

        if (typeof(T) == typeof(Int128) || typeof(T) == typeof(UInt128) || typeof(T) == typeof(Vector128<byte>))
            return Add(data, Unsafe.As<T, Vector128<byte>>(ref value));

        if (typeof(T) == typeof(string))
            return value == null ? Add(data, 0) : AddString(data, ((string)(object)value).AsSpan());
        if (typeof(T) == typeof(Memory<char>))
            return AddString(data, ((Memory<char>)(object)value!).Span);
        if (typeof(T) == typeof(ReadOnlyMemory<char>))
            return AddString(data, ((Memory<char>)(object)value!).Span);
        if (typeof(T) == typeof(Memory<byte>))
            return AddBytes(data, ((Memory<byte>)(object)value!).Span);
        if (typeof(T) == typeof(ReadOnlyMemory<byte>))
            return AddBytes(data, ((Memory<byte>)(object)value!).Span);

        return Add(data, Vector128.CreateScalar(value == null ? 0 : value.GetHashCode()).AsByte());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasher2Data Add<T>(AHasher2Data data, T value, IEqualityComparer<T>? comparer)
    {
        if (comparer == null) return Add(data, value);
        else return Add(data, value == null ? 0 : comparer.GetHashCode(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasher2Data Add<T>(AHasher2Data data, Vector128<T> value)
#if NET7_0
        where T : struct
#endif
        => Add(data, value.AsByte());

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasher2Data Add(AHasher2Data data, Vector128<byte> value)
    {
        data.enc = AesDec(data.enc, value);
        data.sum = ShuffleAndAdd(data.sum, value);
        return data;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasher2Data AddBytes(AHasher2Data data, ReadOnlySpan<byte> value)
    {
        data = AddLength(data, value.Length);

        if (value.Length <= 8)
        {
            data = Add(data, ReadSmall(value));
        }
        else if (value.Length > 32)
        {
            if (value.Length > 64)
            {
                Span<Vector128<byte>> tail = stackalloc Vector128<byte>[4]
#if NET8_0_OR_GREATER
                {
                    Vector128.LoadUnsafe(in value[^(sizeof(ulong) * 2)]),
                    Vector128.LoadUnsafe(in value[^(sizeof(ulong) * 4)]),
                    Vector128.LoadUnsafe(in value[^(sizeof(ulong) * 6)]),
                    Vector128.LoadUnsafe(in value[^(sizeof(ulong) * 8)]),
                };
#else
                {
                    Vector128.LoadUnsafe(ref Unsafe.AsRef(in value[^(sizeof(ulong) * 2)])),
                    Vector128.LoadUnsafe(ref Unsafe.AsRef(in value[^(sizeof(ulong) * 4)])),
                    Vector128.LoadUnsafe(ref Unsafe.AsRef(in value[^(sizeof(ulong) * 6)])),
                    Vector128.LoadUnsafe(ref Unsafe.AsRef(in value[^(sizeof(ulong) * 8)])),
                };
#endif
                Span<Vector128<byte>> current = stackalloc Vector128<byte>[4]
                    { data.key, data.key, data.key, data.key };
                current[0] = AesEnc(current[0], tail[0]);
                current[1] = AesEnc(current[1], tail[1]);
                current[2] = AesEnc(current[2], tail[2]);
                current[3] = AesEnc(current[3], tail[3]);
                Span<Vector128<byte>> sum = stackalloc Vector128<byte>[2] { data.key, ~data.key };
                sum[0] = AddByU64(sum[0], tail[0]);
                sum[1] = AddByU64(sum[1], tail[1]);
                sum[0] = ShuffleAndAdd(sum[0], tail[2]);
                sum[1] = ShuffleAndAdd(sum[1], tail[3]);

                // tail will not use again
                for (; value.Length > 64; value = value[(sizeof(ulong) * 2 * 4)..])
                {
                    var blocks = tail;
#if NET8_0_OR_GREATER
                    blocks[0] = Vector128.LoadUnsafe(in value[0]);
                    blocks[1] = Vector128.LoadUnsafe(in value[sizeof(ulong) * 2]);
                    blocks[2] = Vector128.LoadUnsafe(in value[sizeof(ulong) * 4]);
                    blocks[3] = Vector128.LoadUnsafe(in value[sizeof(ulong) * 6]);
#else
                    blocks[0] = Vector128.LoadUnsafe(ref Unsafe.AsRef(in value[0]));
                    blocks[1] = Vector128.LoadUnsafe(ref Unsafe.AsRef(in value[sizeof(ulong) * 2]));
                    blocks[2] = Vector128.LoadUnsafe(ref Unsafe.AsRef(in value[sizeof(ulong) * 4]));
                    blocks[3] = Vector128.LoadUnsafe(ref Unsafe.AsRef(in value[sizeof(ulong) * 6]));
#endif

                    current[0] = AesEnc(current[0], blocks[0]);
                    current[1] = AesEnc(current[1], blocks[1]);
                    current[2] = AesEnc(current[2], blocks[2]);
                    current[3] = AesEnc(current[3], blocks[3]);
                    sum[0] = ShuffleAndAdd(sum[0], blocks[0]);
                    sum[1] = ShuffleAndAdd(sum[1], blocks[1]);
                    sum[0] = ShuffleAndAdd(sum[0], blocks[2]);
                    sum[1] = ShuffleAndAdd(sum[1], blocks[3]);
                }

                data = Add(data, current[0]);
                data = Add(data, current[1]);
                data = Add(data, current[2]);
                data = Add(data, current[3]);
                data = Add(data, sum[0]);
                data = Add(data, sum[1]);
            }
            else // 33 .. 64
            {
#if NET8_0_OR_GREATER
                var a = Vector128.LoadUnsafe(in value[0]);
                var b = Vector128.LoadUnsafe(in value[sizeof(ulong) * 2]);
                var c = Vector128.LoadUnsafe(in value[^(sizeof(ulong) * 4)]);
                var d = Vector128.LoadUnsafe(in value[^(sizeof(ulong) * 2)]);
#else
                var a = Vector128.LoadUnsafe(ref Unsafe.AsRef(in value[0]));
                var b = Vector128.LoadUnsafe(ref Unsafe.AsRef(in value[sizeof(ulong) * 2]));
                var c = Vector128.LoadUnsafe(ref Unsafe.AsRef(in value[^(sizeof(ulong) * 4)]));
                var d = Vector128.LoadUnsafe(ref Unsafe.AsRef(in value[^(sizeof(ulong) * 2)]));
#endif
                data = Add(data, a);
                data = Add(data, b);
                data = Add(data, c);
                data = Add(data, d);
            }
        }
        else if (value.Length > 16) // 17 .. 32
        {
#if NET8_0_OR_GREATER
            var a = Vector128.LoadUnsafe(in value[0]);
            var b = Vector128.LoadUnsafe(in value[^(sizeof(ulong) * 2)]);
#else
            var a = Vector128.LoadUnsafe(ref Unsafe.AsRef(in value[0]));
            var b = Vector128.LoadUnsafe(ref Unsafe.AsRef(in value[^(sizeof(ulong) * 2)]));
#endif
            data = Add(data, a);
            data = Add(data, b);
        }
        else // 9 .. 16
        {
            var a = Vector128.Create(MemoryMarshal.Cast<byte, ulong>(value)[0],
                MemoryMarshal.Cast<byte, ulong>(value.Slice(value.Length - sizeof(ulong), sizeof(ulong)))[0]);
            data = Add(data, a);
        }

        return data;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasher2Data AddString(AHasher2Data data, ReadOnlySpan<byte> value)
    {
        if (value.Length > 8)
        {
            data = AddBytes(data, value);
            data.enc = AesEnc(data.sum, data.enc);
            data.enc = AesDec(AesDec(data.enc, data.key), data.enc);
        }
        else
        {
            data = AddLength(data, value.Length);

            var a = ReadSmall(value);
            data.sum = ShuffleAndAdd(data.sum, a);
            data.enc = AesEnc(data.sum, data.enc);
            data.enc = AesDec(AesDec(data.enc, data.key), data.enc);
        }
        return data;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasher2Data AddString(AHasher2Data data, ReadOnlySpan<char> value) =>
        AddString(data, MemoryMarshal.Cast<char, byte>(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static int ToHashCode(AHasher2Data data)
    {
        var combined = AesEnc(data.sum, data.enc);
        var result = AesDec(AesDec(combined, data.key), combined).AsInt32();
        return result[0];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static long ToHashCodeLong(AHasher2Data data)
    {
        var combined = AesEnc(data.sum, data.enc);
        var result = AesDec(AesDec(combined, data.key), combined).AsInt64();
        return result[0];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    private static AHasher2Data AddLength(AHasher2Data data, int len)
    {
        data.enc = (data.enc.AsInt32() + Vector128.CreateScalar(len)).AsByte();
        return data;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    private static Vector128<byte> ReadSmall(ReadOnlySpan<byte> value)
    {
        Debug.Assert(value.Length is < 8 and >= 0);
        return value.Length switch
        {
            0 => Vector128<byte>.Zero,
            1 => Vector128.Create((ulong)value[0], value[0]).AsByte(),
            2 or 3 => Vector128.Create((ulong)MemoryMarshal.Cast<byte, ushort>(value)[0], value[^1]).AsByte(),
            _ => Vector128.Create((ulong)MemoryMarshal.Cast<byte, uint>(value)[0],
                    MemoryMarshal.Cast<byte, uint>(value.Slice(value.Length - sizeof(uint), sizeof(uint)))[0])
                .AsByte()
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    private static Vector128<byte> ShuffleAndAdd(Vector128<byte> a, Vector128<byte> b)
    {
        var shuffled = Vector128.Shuffle(a, ShuffleMask);
        return AddByU64(shuffled, b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    private static Vector128<byte> AddByU64(Vector128<byte> a, Vector128<byte> b) =>
        (a.AsUInt64() + b.AsUInt64()).AsByte();

    private static readonly Vector128<byte> ShuffleMask =
        Vector128.Create(
            (byte)0x02, 0x0a, 0x07, 0x00, 0x0c, 0x01, 0x03, 0x0e, 0x05, 0x0f, 0x0d, 0x08, 0x06, 0x09, 0x0b, 0x04);

    #region Combine

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static int Combine<T1>(T1 value1)
    {
        var data = Create(AHasher2._globalRandomState);
        data = Add(data, value1);
        return ToHashCode(data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static int Combine<T1, T2>(T1 value1, T2 value2)
    {
        var data = Create(AHasher2._globalRandomState);
        data = Add(data, value1);
        data = Add(data, value2);
        return ToHashCode(data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static int Combine<T1, T2, T3>(T1 value1, T2 value2, T3 value3)
    {
        var data = Create(AHasher2._globalRandomState);
        data = Add(data, value1);
        data = Add(data, value2);
        data = Add(data, value3);
        return ToHashCode(data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static int Combine<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4)
    {
        var data = Create(AHasher2._globalRandomState);
        data = Add(data, value1);
        data = Add(data, value2);
        data = Add(data, value3);
        data = Add(data, value4);
        return ToHashCode(data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static int Combine<T1, T2, T3, T4, T5>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)
    {
        var data = Create(AHasher2._globalRandomState);
        data = Add(data, value1);
        data = Add(data, value2);
        data = Add(data, value3);
        data = Add(data, value4);
        data = Add(data, value5);
        return ToHashCode(data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static int Combine<T1, T2, T3, T4, T5, T6>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6)
    {
        var data = Create(AHasher2._globalRandomState);
        data = Add(data, value1);
        data = Add(data, value2);
        data = Add(data, value3);
        data = Add(data, value4);
        data = Add(data, value5);
        data = Add(data, value6);
        return ToHashCode(data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static int Combine<T1, T2, T3, T4, T5, T6, T7>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5,
        T6 value6, T7 value7)
    {
        var data = Create(AHasher2._globalRandomState);
        data = Add(data, value1);
        data = Add(data, value2);
        data = Add(data, value3);
        data = Add(data, value4);
        data = Add(data, value5);
        data = Add(data, value6);
        data = Add(data, value7);
        return ToHashCode(data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static int Combine<T1, T2, T3, T4, T5, T6, T7, T8>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5,
        T6 value6, T7 value7, T8 value8)
    {
        var data = Create(AHasher2._globalRandomState);
        data = Add(data, value1);
        data = Add(data, value2);
        data = Add(data, value3);
        data = Add(data, value4);
        data = Add(data, value5);
        data = Add(data, value6);
        data = Add(data, value7);
        data = Add(data, value8);
        return ToHashCode(data);
    }

    #endregion
}

#endif
