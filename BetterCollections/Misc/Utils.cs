using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if NETCOREAPP3_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
#endif

namespace BetterCollections.Misc;

internal static class Utils
{
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
#if NETCOREAPP3_0_OR_GREATER
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
}
