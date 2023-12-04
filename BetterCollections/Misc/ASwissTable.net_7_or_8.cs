#if NET7_0_OR_GREATER
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace BetterCollections.Misc;

public abstract partial class ASwissTable
{
    #region TryH2GetSlot

#if NET8_0_OR_GREATER

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryH2GetSlot(Span<byte> ctrl_bytes, uint start, Vector512<byte> h2, out uint slot)
    {
        Unsafe.SkipInit(out slot);
        var group_size = CtrlGroupSize;
        Debug.Assert(group_size == 16);
        var ctrl_s = MemoryMarshal.Cast<byte, Vector512<byte>>(ctrl_bytes);
        var len = (uint)ctrl_s.Length;
        for (var i = 0u; i < len; i++)
        {
            var nth = i + start;
            if (nth > len) nth -= len;
            var group = ctrl_s[(int)nth];
            if (H2HasSlot(group, h2, out var offset))
            {
                slot = i * group_size + offset;
                return true;
            }
        }
        return false;
    }

#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryH2GetSlot(Span<byte> ctrl_bytes, uint start, Vector256<byte> h2, out uint slot)
    {
        Unsafe.SkipInit(out slot);
        var group_size = CtrlGroupSize;
        Debug.Assert(group_size == 16);
        var ctrl_s = MemoryMarshal.Cast<byte, Vector256<byte>>(ctrl_bytes);
        var len = (uint)ctrl_s.Length;
        for (var i = 0u; i < len; i++)
        {
            var nth = i + start;
            if (nth > len) nth -= len;
            var group = ctrl_s[(int)nth];
            if (H2HasSlot(group, h2, out var offset))
            {
                slot = i * group_size + offset;
                return true;
            }
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryH2GetSlot(Span<byte> ctrl_bytes, uint start, Vector128<byte> h2, out uint slot)
    {
        Unsafe.SkipInit(out slot);
        var group_size = CtrlGroupSize;
        Debug.Assert(group_size == 16);
        var ctrl_s = MemoryMarshal.Cast<byte, Vector128<byte>>(ctrl_bytes);
        var len = (uint)ctrl_s.Length;
        for (var i = 0u; i < len; i++)
        {
            var nth = i + start;
            if (nth > len) nth -= len;
            var group = ctrl_s[(int)nth];
            if (H2HasSlot(group, h2, out var offset))
            {
                slot = i * group_size + offset;
                return true;
            }
        }
        return false;
    }

    #endregion

    #region H2HasSlot

#if NET8_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static bool H2HasSlot(Vector512<byte> group, Vector512<byte> h2, out uint offset)
    {
        Unsafe.SkipInit(out offset);
        var a = Vector512.Equals(group, h2);
        if (a == Vector512<byte>.Zero) return false;
        var bits = a.ExtractMostSignificantBits();
        offset = (uint)ulong.TrailingZeroCount(bits);
        return true;
    }

#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static bool H2HasSlot(Vector256<byte> group, Vector256<byte> h2, out uint offset)
    {
        Unsafe.SkipInit(out offset);
        var a = Vector256.Equals(group, h2);
        if (a == Vector256<byte>.Zero) return false;
        var bits = a.ExtractMostSignificantBits();
        offset = uint.TrailingZeroCount(bits);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static bool H2HasSlot(Vector128<byte> group, Vector128<byte> h2, out uint offset)
    {
        Unsafe.SkipInit(out offset);
        var a = Vector128.Equals(group, h2);
        if (a == Vector128<byte>.Zero) return false;
        var bits = a.ExtractMostSignificantBits();
        offset = uint.TrailingZeroCount(bits);
        return true;
    }

    #endregion
}

#endif
