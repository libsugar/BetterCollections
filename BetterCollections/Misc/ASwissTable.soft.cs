using System;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace BetterCollections.Misc;

public abstract partial class ASwissTable
{
    // reference https://github.com/rust-lang/hashbrown/blob/9f20bd03377ed725ff125db1bcbabf8c11cd81c7/src/raw/generic.rs
    
    #region Consts

    private const ulong ul_x80 = 0x8080808080808080;
    private const ulong ul_x01 = 0x0101010101010101;

    #endregion

    #region MatchH2

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static MatchBits MatchH2(ulong group, ulong h2)
    {
        // https://github.com/rust-lang/hashbrown/blob/9f20bd03377ed725ff125db1bcbabf8c11cd81c7/src/raw/generic.rs#L110
        // https://graphics.stanford.edu/~seander/bithacks.html##ValueInWord
        var cmp = group ^ h2;
        var bits = unchecked(cmp - ul_x01) & ~cmp & ul_x80;
        if (!BitConverter.IsLittleEndian) bits = BinaryPrimitives.ReverseEndianness(bits);
        return new MatchBits(bits, true);
    }

    #endregion

    #region MatchEmpty

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static MatchBits MatchEmpty(ulong group)
    {
        var bits = group & (group << 1) & ul_x80;
        if (!BitConverter.IsLittleEndian) bits = BinaryPrimitives.ReverseEndianness(bits);
        return new MatchBits(bits, true);
    }

    #endregion

    #region MatchEmptyOrDelete

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static MatchBits MatchEmptyOrDelete(ulong group)
    {
        var bits = group & ul_x80;
        if (!BitConverter.IsLittleEndian) bits = BinaryPrimitives.ReverseEndianness(bits);
        return new MatchBits(bits, true);
    }

    #endregion

    #region MatchValue

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static MatchBits MatchValue(ulong group) => MatchEmptyOrDelete(~group);

    #endregion
}
