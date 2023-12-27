#if NET7_0_OR_GREATER
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
    public static Vector128<byte> ToVector<T>(T value)
    {
        var type = typeof(T);
        if (type == typeof(int)) return Vector128.CreateScalar(Unsafe.As<T, int>(ref value)).AsByte();
        else if (type == typeof(uint)) return Vector128.CreateScalar(Unsafe.As<T, uint>(ref value)).AsByte();
        else if (type == typeof(long)) return Vector128.CreateScalar(Unsafe.As<T, long>(ref value)).AsByte();
        else if (type == typeof(ulong)) return Vector128.CreateScalar(Unsafe.As<T, ulong>(ref value)).AsByte();
        else if (type == typeof(Int128)) return Unsafe.As<T, Vector128<byte>>(ref value);
        else if (type == typeof(UInt128)) return Unsafe.As<T, Vector128<byte>>(ref value);
        else if (type == typeof(Vector128<byte>)) return Unsafe.As<T, Vector128<byte>>(ref value);
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Vector128<>))
            return Unsafe.As<T, Vector128<byte>>(ref value);
        else return Vector128.CreateScalar(value == null ? 0 : value.GetHashCode()).AsByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static void Add(ref AHasher2Data data, int value) => Add(ref data, (long)value);

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static void Add(ref AHasher2Data data, uint value) => Add(ref data, (ulong)value);

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static void Add(ref AHasher2Data data, long value) => Add(ref data, Vector128.CreateScalar(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static void Add(ref AHasher2Data data, ulong value) => Add(ref data, Vector128.CreateScalar(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static void Add(ref AHasher2Data data, Int128 value) =>
        Add(ref data, Unsafe.As<Int128, Vector128<byte>>(ref value));

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static void Add(ref AHasher2Data data, UInt128 value) =>
        Add(ref data, Unsafe.As<UInt128, Vector128<byte>>(ref value));

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static void Add<T>(ref AHasher2Data data, T value)
    {
        var type = typeof(T);
        if (type == typeof(int)) Add(ref data, Unsafe.As<T, int>(ref value));
        else if (type == typeof(uint)) Add(ref data, Unsafe.As<T, uint>(ref value));
        else if (type == typeof(long)) Add(ref data, Unsafe.As<T, long>(ref value));
        else if (type == typeof(ulong)) Add(ref data, Unsafe.As<T, ulong>(ref value));
        else if (type == typeof(Int128)) Add(ref data, Unsafe.As<T, Int128>(ref value));
        else if (type == typeof(UInt128)) Add(ref data, Unsafe.As<T, UInt128>(ref value));
        else if (type == typeof(Vector128<byte>)) Add(ref data, Unsafe.As<T, Vector128<byte>>(ref value));
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Vector128<>))
            Add(ref data, Unsafe.As<T, Vector128<byte>>(ref value));
        else Add(ref data, value == null ? 0 : value.GetHashCode());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static void Add<T>(ref AHasher2Data data, T value, IEqualityComparer<T>? comparer)
    {
        if (comparer == null) Add(ref data, value);
        else Add(ref data, value == null ? 0 : comparer.GetHashCode(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static void Add<T>(ref AHasher2Data data, Vector128<T> value)
#if NET7_0
        where T : struct
#endif
        => Add(ref data, value.AsByte());

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static void Add(ref AHasher2Data data, Vector128<byte> value)
    {
        data.enc = AesDec(data.enc, value);
        data.sum = ShuffleAndAdd(data.sum, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static void AddBytes(ref AHasher2Data data, ReadOnlySpan<byte> value)
    {
        AddLength(ref data, value.Length);

        if (value.Length <= 8)
        {
            Add(ref data, ReadSmall(value));
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

                Add(ref data, current[0]);
                Add(ref data, current[1]);
                Add(ref data, current[2]);
                Add(ref data, current[3]);
                Add(ref data, sum[0]);
                Add(ref data, sum[1]);
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
                Add(ref data, a);
                Add(ref data, b);
                Add(ref data, c);
                Add(ref data, d);
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
            Add(ref data, a);
            Add(ref data, b);
        }
        else // 9 .. 16
        {
            var a = Vector128.Create(MemoryMarshal.Cast<byte, ulong>(value)[0],
                MemoryMarshal.Cast<byte, ulong>(value.Slice(value.Length - sizeof(ulong), sizeof(ulong)))[0]);
            Add(ref data, a);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static void AddString(ref AHasher2Data data, ReadOnlySpan<byte> value)
    {
        if (value.Length > 8)
        {
            AddBytes(ref data, value);
            data.enc = AesEnc(data.sum, data.enc);
            data.enc = AesDec(AesDec(data.enc, data.key), data.enc);
        }
        else
        {
            AddLength(ref data, value.Length);

            var a = ReadSmall(value);
            data.sum = ShuffleAndAdd(data.sum, a);
            data.enc = AesEnc(data.sum, data.enc);
            data.enc = AesDec(AesDec(data.enc, data.key), data.enc);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static void AddString(ref AHasher2Data data, ReadOnlySpan<char> value) =>
        AddString(ref data, MemoryMarshal.Cast<char, byte>(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static int ToHashCode(ref AHasher2Data data)
    {
        var combined = AesEnc(data.sum, data.enc);
        var result = AesDec(AesDec(combined, data.key), combined).AsInt32();
        return result[0];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static long ToHashCodeLong(ref AHasher2Data data)
    {
        var combined = AesEnc(data.sum, data.enc);
        var result = AesDec(AesDec(combined, data.key), combined).AsInt64();
        return result[0];
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
    private static void AddLength(ref AHasher2Data data, int len)
    {
        data.enc = (data.enc.AsInt32() + Vector128.CreateScalar(len)).AsByte();
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
        var target1 = ToVector(value1);
        data.enc = AesDec(data.enc, target1);
        data.sum = ShuffleAndAdd(data.sum, target1);
        var combined = AesEnc(data.sum, data.enc);
        var result = AesDec(AesDec(combined, data.key), combined).AsInt32();
        return result[0];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static int Combine<T1, T2>(T1 value1, T2 value2)
    {
        var data = Create(AHasher2._globalRandomState);
        var target1 = ToVector(value1);
        data.enc = AesDec(data.enc, target1);
        data.sum = ShuffleAndAdd(data.sum, target1);
        var target2 = ToVector(value2);
        data.enc = AesDec(data.enc, target2);
        data.sum = ShuffleAndAdd(data.sum, target2);
        var combined = AesEnc(data.sum, data.enc);
        var result = AesDec(AesDec(combined, data.key), combined).AsInt32();
        return result[0];
    }

    #endregion
}

#endif
