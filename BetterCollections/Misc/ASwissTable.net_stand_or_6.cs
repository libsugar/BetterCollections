#if NETSTANDARD || NET6_0
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BetterCollections.Misc;

public abstract partial class ASwissTable
{
    #region TryH2GetSlot

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryH2GetSlot(Span<byte> ctrl_bytes, uint start, ulong h2, out uint slot)
    {
        Unsafe.SkipInit(out slot);
        var group_size = CtrlGroupSize;
        Debug.Assert(group_size == 16);
        var ctrl_s = MemoryMarshal.Cast<byte, ulong>(ctrl_bytes);
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
    protected static unsafe bool H2HasSlot(ulong group, ulong h2, out uint offset)
    {
        Unsafe.SkipInit(out offset);
        for (var j = 0u; j < CtrlGroupSize; j++)
        {
            if (((byte*)&group)[j] == ((byte*)&h2)[j])
            {
                offset = j;
                return true;
            }
        }
        return false;
    }

    #endregion
}

#endif
