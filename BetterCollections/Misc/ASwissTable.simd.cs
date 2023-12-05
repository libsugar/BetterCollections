#if NET7_0_OR_GREATER
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace BetterCollections.Misc;

public abstract partial class ASwissTable
{
    #region MatchH2

#if NET8_0_OR_GREATER

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static MatchBits MatchH2(Vector512<byte> group, Vector512<byte> h2) =>
        MatchEmptyOrDelete(Vector512.Equals(group, h2));

#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static MatchBits MatchH2(Vector256<byte> group, Vector256<byte> h2) =>
        MatchEmptyOrDelete(Vector256.Equals(group, h2));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static MatchBits MatchH2(Vector128<byte> group, Vector128<byte> h2) =>
        MatchEmptyOrDelete(Vector128.Equals(group, h2));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static MatchBits MatchH2(Vector64<byte> group, Vector64<byte> h2) =>
        MatchEmptyOrDelete(Vector64.Equals(group, h2));

    #endregion

    #region MatchEmpty

#if NET8_0_OR_GREATER

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static MatchBits MatchEmpty(Vector512<byte> group) => MatchH2(group, Vector512<byte>.AllBitsSet);

#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static MatchBits MatchEmpty(Vector256<byte> group) => MatchH2(group, Vector256<byte>.AllBitsSet);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static MatchBits MatchEmpty(Vector128<byte> group) => MatchH2(group, Vector128<byte>.AllBitsSet);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static MatchBits MatchEmpty(Vector64<byte> group) => MatchH2(group, Vector64<byte>.AllBitsSet);

    #endregion

    #region MatchEmptyOrDelete

#if NET8_0_OR_GREATER

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static MatchBits MatchEmptyOrDelete(Vector512<byte> group) => new(group.ExtractMostSignificantBits());

#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static MatchBits MatchEmptyOrDelete(Vector256<byte> group) => new(group.ExtractMostSignificantBits());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static MatchBits MatchEmptyOrDelete(Vector128<byte> group) => new(group.ExtractMostSignificantBits());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static MatchBits MatchEmptyOrDelete(Vector64<byte> group) => new(group.ExtractMostSignificantBits());

    #endregion

    #region MatchValue

#if NET8_0_OR_GREATER

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static MatchBits MatchValue(Vector512<byte> group) => MatchEmptyOrDelete(~group);

#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static MatchBits MatchValue(Vector256<byte> group) => MatchEmptyOrDelete(~group);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static MatchBits MatchValue(Vector128<byte> group) => MatchEmptyOrDelete(~group);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static MatchBits MatchValue(Vector64<byte> group) => MatchEmptyOrDelete(~group);

    #endregion
}

#endif
