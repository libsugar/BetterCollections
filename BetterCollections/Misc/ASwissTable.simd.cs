#if NET7_0_OR_GREATER
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace BetterCollections.Misc;

public abstract partial class ASwissTable
{
    #region MatchH2

#if NET8_0_OR_GREATER

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static ulong MatchH2(in Vector512<byte> group, byte h2) =>
        Vector512.Equals(group, Vector512.Create(h2)).ExtractMostSignificantBits();

#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static uint MatchH2(in Vector256<byte> group, byte h2) =>
        Vector256.Equals(group, Vector256.Create(h2)).ExtractMostSignificantBits();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static uint MatchH2(in Vector128<byte> group, byte h2) =>
        Vector128.Equals(group, Vector128.Create(h2)).ExtractMostSignificantBits();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static uint MatchH2(in Vector64<byte> group, byte h2) =>
        Vector64.Equals(group, Vector64.Create(h2)).ExtractMostSignificantBits();

    #endregion

    #region MatchEmpty

#if NET8_0_OR_GREATER

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static ulong MatchEmpty(in Vector512<byte> group) =>
        Vector512.Equals(group, Vector512<byte>.AllBitsSet).ExtractMostSignificantBits();

#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static uint MatchEmpty(in Vector256<byte> group) =>
        Vector256.Equals(group, Vector256<byte>.AllBitsSet).ExtractMostSignificantBits();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static uint MatchEmpty(in Vector128<byte> group) =>
        Vector128.Equals(group, Vector128<byte>.AllBitsSet).ExtractMostSignificantBits();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static uint MatchEmpty(in Vector64<byte> group) =>
        Vector64.Equals(group, Vector64<byte>.AllBitsSet).ExtractMostSignificantBits();

    #endregion

    #region MatchEmptyOrDelete

#if NET8_0_OR_GREATER

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static ulong MatchEmptyOrDelete(in Vector512<byte> group) =>
        group.ExtractMostSignificantBits();

#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static uint MatchEmptyOrDelete(in Vector256<byte> group) =>
        group.ExtractMostSignificantBits();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static uint MatchEmptyOrDelete(in Vector128<byte> group) =>
        group.ExtractMostSignificantBits();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static uint MatchEmptyOrDelete(in Vector64<byte> group) =>
        group.ExtractMostSignificantBits();

    #endregion

    #region MatchValue

#if NET8_0_OR_GREATER

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static ulong MatchValue(in Vector512<byte> group) =>
        (~group).ExtractMostSignificantBits();

#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static uint MatchValue(in Vector256<byte> group) =>
        (~group).ExtractMostSignificantBits();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static uint MatchValue(in Vector128<byte> group) =>
        (~group).ExtractMostSignificantBits();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static uint MatchValue(in Vector64<byte> group) =>
        (~group).ExtractMostSignificantBits();

    #endregion
}

#endif
