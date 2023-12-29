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

        return Add(data, Vector128.CreateScalar(value?.GetHashCode() ?? 0).AsByte());
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
            return AddBytes_len_8(data, value);

            [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
            static AHasher2Data AddBytes_len_8(AHasher2Data data, ReadOnlySpan<byte> value)
                => Add(data, ReadSmall(value));
        }
        else if (value.Length > 32)
        {
            if (value.Length > 64)
            {
                return AddBytes_len_64(data, value);

                [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
                static AHasher2Data AddBytes_len_64(AHasher2Data data, ReadOnlySpan<byte> value)
                {
                    var t0 = Vector128.LoadUnsafe(in value[^(sizeof(ulong) * 2)]);
                    var s0 = AddByU64(data.key, t0);
                    var c0 = AesEnc(data.key, t0);
                    var t2 = Vector128.LoadUnsafe(in value[^(sizeof(ulong) * 6)]);
                    s0 = ShuffleAndAdd(s0, t2);
                    var c2 = AesEnc(data.key, t2);
                    var t1 = Vector128.LoadUnsafe(in value[^(sizeof(ulong) * 4)]);
                    var s1 = AddByU64(~data.key, t1);
                    var c1 = AesEnc(data.key, t1);
                    var t3 = Vector128.LoadUnsafe(in value[^(sizeof(ulong) * 8)]);
                    s1 = ShuffleAndAdd(s1, t3);
                    var c3 = AesEnc(data.key, t3);

                    for (var i = 0; i + sizeof(ulong) * 2 * 4 < value.Length; i += sizeof(ulong) * 2 * 4)
                    {
                        var b0 = Vector128.LoadUnsafe(
                            in Unsafe.Add(ref Unsafe.AsRef(in value[0]), i));
                        c0 = AesEnc(c0, b0);
                        s0 = ShuffleAndAdd(s0, b0);
                        var b1 = Vector128.LoadUnsafe(
                            in Unsafe.Add(ref Unsafe.AsRef(in value[0]), i + sizeof(ulong) * 2));
                        c1 = AesEnc(c1, b1);
                        s1 = ShuffleAndAdd(s1, b1);
                        var b2 = Vector128.LoadUnsafe(
                            in Unsafe.Add(ref Unsafe.AsRef(in value[0]), i + sizeof(ulong) * 4));
                        c2 = AesEnc(c2, b2);
                        s0 = ShuffleAndAdd(s0, b2);
                        var b3 = Vector128.LoadUnsafe(
                            in Unsafe.Add(ref Unsafe.AsRef(in value[0]), i + sizeof(ulong) * 6));
                        c3 = AesEnc(c3, b3);
                        s1 = ShuffleAndAdd(s1, b3);
                    }

                    data = Add(data, c0);
                    data = Add(data, c1);
                    data = Add(data, c2);
                    data = Add(data, c3);
                    data = Add(data, s0);
                    data = Add(data, s1);
                    return data;
                }
            }
            else // 33 .. 64
            {
                return AddBytes_len_33_64(data, value);

                [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
                static AHasher2Data AddBytes_len_33_64(AHasher2Data data, ReadOnlySpan<byte> value)
                {
                    var a = Vector128.LoadUnsafe(in value[0]);
                    var b = Vector128.LoadUnsafe(in value[sizeof(ulong) * 2]);
                    var c = Vector128.LoadUnsafe(in value[^(sizeof(ulong) * 4)]);
                    var d = Vector128.LoadUnsafe(in value[^(sizeof(ulong) * 2)]);
                    data = Add(data, a);
                    data = Add(data, b);
                    data = Add(data, c);
                    data = Add(data, d);
                    return data;
                }
            }
        }
        else if (value.Length > 16) // 17 .. 32
        {
            return AddBytes_len_17_32(data, value);

            [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
            static AHasher2Data AddBytes_len_17_32(AHasher2Data data, ReadOnlySpan<byte> value)
            {
                var a = Vector128.LoadUnsafe(in value[0]);
                var b = Vector128.LoadUnsafe(in value[^(sizeof(ulong) * 2)]);
                data = Add(data, a);
                data = Add(data, b);
                return data;
            }
        }
        else // 9 .. 16
        {
            return AddBytes_len_9_16(data, value);

            [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
            static AHasher2Data AddBytes_len_9_16(AHasher2Data data, ReadOnlySpan<byte> value)
            {
                ref var addr = ref Unsafe.AsRef(in value[0]);
                var a = Unsafe.As<byte, ulong>(ref addr);
                var b = Unsafe.As<byte, ulong>(ref Unsafe.Add(ref addr, value.Length - sizeof(ulong)));
                var v = Vector128.Create(a, b).AsByte();
                return Add(data, v);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasher2Data AddString(AHasher2Data data, ReadOnlySpan<byte> value)
    {
        if (value.Length > 8)
        {
            return AddString_large(data, value);

            [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
            static AHasher2Data AddString_large(AHasher2Data data, ReadOnlySpan<byte> value)
            {
                data = AddBytes(data, value);
                data.enc = AesEnc(data.sum, data.enc);
                data.enc = AesDec(AesDec(data.enc, data.key), data.enc);
                return data;
            }
        }
        else
        {
            return AddString_small(data, value);

            [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
            static AHasher2Data AddString_small(AHasher2Data data, ReadOnlySpan<byte> value)
            {
                data = AddLength(data, value.Length);

                var a = ReadSmall(value);
                data.sum = ShuffleAndAdd(data.sum, a);
                data.enc = AesEnc(data.sum, data.enc);
                data.enc = AesDec(AesDec(data.enc, data.key), data.enc);

                return data;
            }
        }
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
        ref var addr = ref Unsafe.AsRef(in value[0]);
        return value.Length switch
        {
            0 => Vector128<byte>.Zero,
            1 => Vector128.CreateScalar(addr).AsByte(),
            2 => Vector128.CreateScalar(Unsafe.As<byte, ushort>(ref addr)).AsByte(),
            3 => Vector128.Create(Unsafe.As<byte, ushort>(ref addr), Unsafe.Add(ref addr, 2)).AsByte(),
            4 => Vector128.CreateScalar(Unsafe.As<byte, uint>(ref addr)).AsByte(),
            5 => Vector128.Create(Unsafe.As<byte, uint>(ref addr),
                Unsafe.As<byte, uint>(ref Unsafe.Add(ref addr, 5 - sizeof(uint)))).AsByte(),
            6 => Vector128.Create(Unsafe.As<byte, uint>(ref addr),
                Unsafe.As<byte, uint>(ref Unsafe.Add(ref addr, 6 - sizeof(uint)))).AsByte(),
            7 => Vector128.Create(Unsafe.As<byte, uint>(ref addr),
                Unsafe.As<byte, uint>(ref Unsafe.Add(ref addr, 7 - sizeof(uint)))).AsByte(),
            _ => throw new ArgumentOutOfRangeException(nameof(value)),
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
