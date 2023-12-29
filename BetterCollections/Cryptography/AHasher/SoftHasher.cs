#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace BetterCollections.Cryptography.AHasherImpl;

public static class SoftHasher
{
    private static readonly Vector128<ulong> PI_1 = Vector128.Create(0x243f_6a88_85a3_08d3UL, 0x1319_8a2e_0370_7344UL);
    private static readonly Vector128<ulong> PI_2 = Vector128.Create(0xa409_3822_299f_31d0UL, 0x082e_fa98_ec4e_6c89UL);

    /// <summary>
    /// This constant comes from Kunth's prng (Empirically it works better than those from splitmix32)
    /// </summary>
    private const ulong MULTIPLE = 6364136223846793005;

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasher2Data Create(AHasherRandomState randomState)
    {
        var key1 = randomState.key1.AsUInt64() ^ PI_1;
        var key2 = randomState.key2.AsUInt64() ^ PI_2;
        return new AHasher2Data(key1.GetElement(0), key1.GetElement(1), key2.AsByte());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization), SkipLocalsInit]
    public static AHasher2Data Add(AHasher2Data data, int value) => Update(data, (ulong)value);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization), SkipLocalsInit]
    public static AHasher2Data Add(AHasher2Data data, uint value) => Update(data, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization), SkipLocalsInit]
    public static AHasher2Data Add(AHasher2Data data, long value) => Update(data, (ulong)value);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization), SkipLocalsInit]
    public static AHasher2Data Add(AHasher2Data data, ulong value) => Update(data, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization), SkipLocalsInit]
    public static AHasher2Data Add<T>(AHasher2Data data, T value)
    {
        return Update(data, (ulong)(value?.GetHashCode() ?? 0));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasher2Data Add<T>(AHasher2Data data, T value, IEqualityComparer<T>? comparer)
    {
        if (comparer == null) return Add(data, value);
        else return Update(data, (ulong)(value == null ? 0 : comparer.GetHashCode(value)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasher2Data AddBytes(AHasher2Data data, ReadOnlySpan<byte> value)
    {
        data.buffer = unchecked((data.buffer + (ulong)value.Length) * MULTIPLE);

        if (value.Length > 8)
        {
            if (value.Length > 16)
            {
                return AddBytes_len_large(data, value);

                [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
                static AHasher2Data AddBytes_len_large(AHasher2Data data, ReadOnlySpan<byte> value)
                {
                    ref var addr = ref Unsafe.AsRef(in value[0]);
                    var tail = Vector128.LoadUnsafe(in Unsafe.Add(ref addr, value.Length - sizeof(ulong) * 2));
                    data = UpdateLarge(data, tail);

                    for (var i = 0; i + sizeof(ulong) * 2 < value.Length; i += sizeof(ulong) * 2)
                    {
                        var item = Vector128.LoadUnsafe(in Unsafe.Add(ref addr, i));
                        data = UpdateLarge(data, item);
                    }

                    return data;
                }
            }
            else
            {
                return AddBytes_len_16(data, value);

                [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
                static AHasher2Data AddBytes_len_16(AHasher2Data data, ReadOnlySpan<byte> value)
                {
                    ref var addr = ref Unsafe.AsRef(in value[0]);
                    var a = Unsafe.As<byte, ulong>(ref addr);
                    var b = Unsafe.As<byte, ulong>(ref Unsafe.Add(ref addr, value.Length - sizeof(ulong)));
                    var v = Vector128.Create(a, b).AsByte();
                    return UpdateLarge(data, v);
                }
            }
        }
        else
        {
            return AddBytes_len_8(data, value);

            [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
            static AHasher2Data AddBytes_len_8(AHasher2Data data, ReadOnlySpan<byte> value) =>
                UpdateLarge(data, ReadSmall(value));
        }

        return data;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasher2Data AddString(AHasher2Data data, ReadOnlySpan<byte> value)
    {
        if (value.Length > 8)
        {
            return AddBytes(data, value);
        }
        else
        {
            return AddString_len_8(data, value);

            [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
            static AHasher2Data AddString_len_8(AHasher2Data data, ReadOnlySpan<byte> value)
            {
                var a = ReadSmall(value).AsUInt64() ^ data.buffer_extra1;
                data.buffer = FoldedMultiply(a.GetElement(0), a.GetElement(1));
                data.pad = unchecked(data.pad + (ulong)value.Length);
                return data ;
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasher2Data AddString(AHasher2Data data, ReadOnlySpan<char> value) =>
        AddString(data, MemoryMarshal.Cast<char, byte>(value));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static int ToHashCode(AHasher2Data data)
        => (int)ToHashCodeLong(data);

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static long ToHashCodeLong(AHasher2Data data)
    {
        var rot = (int)(data.buffer & 63);
        return (long)BitOperations.RotateLeft(FoldedMultiply(data.buffer, data.pad), rot);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    private static AHasher2Data Update(AHasher2Data data, ulong value)
    {
        data.buffer = FoldedMultiply(value ^ data.buffer);
        return data;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    private static AHasher2Data UpdateLarge(AHasher2Data data, Vector128<byte> value)
    {
        var a = value.AsUInt64() ^ data.extra.AsUInt64();
        var combined = FoldedMultiply(a.GetElement(0), a.GetElement(1));
        data.buffer = BitOperations.RotateLeft(unchecked(data.buffer + data.pad) ^ combined, 23);
        return data;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong FoldedMultiply(ulong s)
    {
        var result = unchecked((UInt128)s * MULTIPLE);
        return (ulong)(result & 0xffff_ffff_ffff_ffff) ^ (ulong)(result >> 64);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong FoldedMultiply(ulong s, ulong by)
    {
        var result = unchecked((UInt128)s * by);
        return (ulong)(result & 0xffff_ffff_ffff_ffff) ^ (ulong)(result >> 64);
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
}

#endif
