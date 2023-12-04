#if NETSTANDARD || NET6_0
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if NET6_0
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
#endif

namespace BetterCollections.Misc;

public abstract partial class ASwissTable
{
    #region TryH2GetSlot

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryH2GetSlot(Span<byte> ctrl_bytes, uint start, Vector<byte> h2, out uint slot)
    {
        Unsafe.SkipInit(out slot);
        var group_size = CtrlGroupSize;
        Debug.Assert(group_size == 16);
        var ctrl_s = MemoryMarshal.Cast<byte, Vector<byte>>(ctrl_bytes);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static bool H2HasSlot(Vector<byte> group, Vector<byte> h2, out uint offset)
    {
        Unsafe.SkipInit(out offset);
#if NET6_0
        if (AdvSimd.IsSupported)
        {
            var a = AdvSimd.CompareEqual(group.AsVector128(), h2.AsVector128());
            var bits = AdvSimd.Extract(a, 0);
            if (bits == 0) return false;
            offset = Utils.TrailingZeroCount(bits);
            return true;
        }
        else
        {
            var a = Vector.Equals(group, h2);
            if (a == Vector<byte>.Zero) return false;
            if (Avx2.IsSupported)
            {
                var v = a.AsVector256();
                var bits = Avx2.MoveMask(v);
                offset = Utils.TrailingZeroCount((uint)bits);
                return true;
            }
            else if (Sse2.IsSupported)
            {
                var v = a.AsVector128();
                var bits = Sse2.MoveMask(v);
                offset = Utils.TrailingZeroCount((uint)bits);
                return true;
            }

            if (Fallback(a, out offset)) return true;
        }
#else
        var a = Vector.Equals(group, h2);
        if (a == Vector<byte>.Zero) return false;
        if (Fallback(a, out offset)) return true;
#endif
        return false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Fallback(Vector<byte> a, out uint offset)
        {
            Unsafe.SkipInit(out offset);
            for (var j = 0u; j < CtrlGroupSize; j++)
            {
                if (a[(int)j] != 0)
                {
                    offset = j;
                    return true;
                }
            }
            return false;
        }
    }

    #endregion
}

#endif
