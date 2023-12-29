using System;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
#if NETCOREAPP3_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
#endif

namespace BetterCollections.Misc;

public static partial class Utils
{
    /// <summary>
    /// Create a ulong where all bytes are the specified value
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe ulong CreateULong(byte v)
    {
#if NET6_0_OR_GREATER
        var v64 = Vector64.Create(v);
        return *(ulong*)&v64;
#else
        var bytes = stackalloc byte[sizeof(ulong)] { v, v, v, v, v, v, v, v };
        return *(ulong*)bytes;
#endif
    }

    /// <summary>
    /// Round up to the nearest power of 2
    /// </summary>
    /// <param name="value">must > 0 &amp;&amp; &lt; uint.MaxValue / 2 + 1</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CeilPowerOf2(this int value)
    {
#if NET7_0_OR_GREATER
        return 1 << (32 - int.LeadingZeroCount(value - 1));
#else
        var v = value;
        v--;
        v |= v >> 1;
        v |= v >> 2;
        v |= v >> 4;
        v |= v >> 8;
        v |= v >> 16;
        v++;
        return v;
#endif
    }

    /// <summary>
    /// Round up to the nearest power of 2
    /// </summary>
    /// <param name="value">must > 0 &amp;&amp; &lt; uint.MaxValue / 2 + 1</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint CeilPowerOf2(this uint value)
    {
#if NET7_0_OR_GREATER
        return 1u << (32 - (int)uint.LeadingZeroCount(value - 1));
#else
        var v = value;
        v--;
        v |= v >> 1;
        v |= v >> 2;
        v |= v >> 4;
        v |= v >> 8;
        v |= v >> 16;
        v++;
        return v;
#endif
    }

    /// <summary>
    /// Round up to power of 2
    /// </summary>
    /// <param name="value">Number to round up</param>
    /// <param name="binary">Must be power of 2, no check, need manually ensure</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CeilBinary(this int value, int binary) => (value + (binary - 1)) & ~(binary - 1);

    /// <summary>
    /// Round up to power of 2
    /// </summary>
    /// <param name="value">Number to round up</param>
    /// <param name="binary">Must be power of 2, no check, need manually ensure</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint CeilBinary(this uint value, uint binary) => (value + (binary - 1)) & ~(binary - 1);

    private static ReadOnlySpan<byte> TrailingZeroCountDeBruijn => new byte[32]
    {
        00, 01, 28, 02, 29, 14, 24, 03,
        30, 22, 20, 15, 25, 17, 04, 08,
        31, 27, 13, 23, 21, 19, 16, 07,
        26, 12, 18, 06, 11, 05, 10, 09
    };

    /// <summary>
    /// Count the number of trailing zero bits in an integer value.
    /// Similar in behavior to the x86 instruction TZCNT.
    /// </summary>
    /// <param name="value">The value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint TrailingZeroCount(uint value)
    {
#if NET7_0_OR_GREATER
        return (uint)BitOperations.TrailingZeroCount(value);
#else
#if NET6_0_OR_GREATER
        if (Bmi1.IsSupported)
        {
            // TZCNT contract is 0->32
            return Bmi1.TrailingZeroCount(value);
        }

        if (ArmBase.IsSupported)
        {
            return (uint)ArmBase.LeadingZeroCount(ArmBase.ReverseElementBits(value));
        }
#endif
        // Unguarded fallback contract is 0->0, BSF contract is 0->undefined
        if (value == 0)
        {
            return 32;
        }

        // uint.MaxValue >> 27 is always in range [0 - 31] so we use Unsafe.AddByteOffset to avoid bounds check
        return Unsafe.AddByteOffset(
            // Using deBruijn sequence, k=2, n=5 (2^5=32) : 0b_0000_0111_0111_1100_1011_0101_0011_0001u
            ref MemoryMarshal.GetReference(TrailingZeroCountDeBruijn),
            // uint|long -> IntPtr cast on 32-bit platforms does expensive overflow checks not needed here
            (IntPtr)(int)(((value & (uint)-(int)value) * 0x077CB531u) >> 27)); // Multi-cast mitigates redundant conv.u8
#endif
    }

    /// <summary>
    /// Count the number of trailing zero bits in an integer value.
    /// Similar in behavior to the x86 instruction TZCNT.
    /// </summary>
    /// <param name="value">The value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint TrailingZeroCount(ulong value)
    {
#if NET7_0_OR_GREATER
        return (uint)BitOperations.TrailingZeroCount(value);
#else
#if NET6_0_OR_GREATER
        if (Bmi1.X64.IsSupported)
        {
            // TZCNT contract is 0->64
            return (uint)Bmi1.X64.TrailingZeroCount(value);
        }

        if (ArmBase.Arm64.IsSupported)
        {
            return (uint)ArmBase.Arm64.LeadingZeroCount(ArmBase.Arm64.ReverseElementBits(value));
        }
#endif
        uint lo = (uint)value;

        if (lo == 0)
        {
            return 32 + TrailingZeroCount((uint)(value >> 32));
        }

        return TrailingZeroCount(lo);
#endif
    }

#if NET8_0_OR_GREATER
    [GeneratedRegex(".{8}(?!$)")]
    private static partial Regex SplitBytes();
#endif

    public static string ToBinaryString(this ulong value)
    {
        var a = Convert.ToString((long)value, 2).PadLeft(64, '0');
#if NET8_0_OR_GREATER
        return SplitBytes().Replace(a, "$0_");
#else
        return Regex.Replace(a, ".{8}(?!$)", "$0_");
#endif
    }
    
    public static string ToBinaryString(this uint value)
    {
        var a = Convert.ToString((int)value, 2).PadLeft(32, '0');
#if NET8_0_OR_GREATER
        return SplitBytes().Replace(a, "$0_");
#else
        return Regex.Replace(a, ".{8}(?!$)", "$0_");
#endif
    }

    /// <summary>
    /// Evaluate whether a given integral value is a power of 2.
    /// </summary>
    /// <param name="value">The value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPow2(long value) => (value & (value - 1)) == 0 && value > 0;

    /// <summary>
    /// Evaluate whether a given integral value is a power of 2.
    /// </summary>
    /// <param name="value">The value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPow2(ulong value) => (value & (value - 1)) == 0 && value > 0;

#pragma warning disable CS0649
    private struct AlignmentHelper<T> where T : unmanaged
    {
        public byte Padding;
        public T Target;
    }
#pragma warning restore CS0649

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int AlignmentOf<T>() where T : unmanaged
    {
        return (int)Marshal.OffsetOf<AlignmentHelper<T>>(nameof(AlignmentHelper<T>.Target));
    }
}
