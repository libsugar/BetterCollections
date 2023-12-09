using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BetterCollections.Buffers;
using BetterCollections.Cryptography;
#if NET7_0_OR_GREATER
using System.Runtime.Intrinsics;
#endif

namespace BetterCollections;

public partial class IndirectHashMap<TKey, TValue>
{
    #region Consts

    protected const byte SlotIsEmpty = 0b1111_1111;
    protected const byte SlotIsDeleted = 0b1000_0000;
    protected const byte SlotValueMask = 0b0111_1111;

    protected const int DefaultCapacity = 4;

    #endregion

    #region Fields

    private readonly IEqualityComparer<TKey>? comparer;
    private readonly ArrayPool<byte> poolBytes;
    private readonly ArrayPool<Entry> poolEntries;
    private Table table;
    private int count;
    private int version;
    private readonly AHasher hasher;

    private struct Table
    {
        public byte[]? ctrlArray;
        public Entry[]? entryArray;
        public int slotSizeMinusOne;
        public int growthCount;
        public GroupType groupType;
    }

    #endregion

    #region Ctors

    public IndirectHashMap() : this(0) { }

    public IndirectHashMap(int cap = 0, IEqualityComparer<TKey>? comparer = null)
        : this(ArrayPoolFactory.DirectAllocation, cap, comparer) { }

    public IndirectHashMap(ArrayPoolFactory poolFactory, int cap = 0, IEqualityComparer<TKey>? comparer = null)
    {
        if (cap < 0) throw new ArgumentOutOfRangeException(nameof(cap), "Capacity must be >= 0");
        poolBytes = poolFactory.GetMayUninitialized<byte>();
        poolEntries = poolFactory.Get<Entry>();
        if (!typeof(TKey).IsValueType) comparer ??= EqualityComparer<TKey>.Default;
        this.comparer = comparer;
        count = 0;
        version = 0;
        hasher = new AHasher();
#if NET7_0_OR_GREATER // todo auto choose
        Vector256Impl.InitTable(this, cap, ref table, true);
#else
        throw new NotSupportedException("todo supported");
#endif
    }

    #endregion

    #region Group Type

    private enum GroupType
    {
        ULong,
#if NET7_0_OR_GREATER
        Vector64,
        Vector128,
        Vector256,
#if NET8_0_OR_GREATER
        Vector512,
#endif
#endif
    }

    #endregion

    #region Entry

    private struct Entry(TKey Key, TValue Value, uint hashCode)
    {
        public readonly TKey Key = Key;
        public TValue Value = Value;
        public readonly uint HashCode = hashCode;

        public override string ToString() => $"Entry({Key}, {Value}) {{ HashCode = {HashCode} }}";
    }

    #endregion

    #region CalcHash

#if NET6_0_OR_GREATER
    [SkipLocalsInit]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CalcHash(TKey key, out uint h1, out byte h2)
    {
        var systemHashCode = typeof(TKey).IsValueType && comparer == null
            ? key.GetHashCode()
            : !typeof(TKey).IsValueType
                ? key == null! ? 0 : comparer!.GetHashCode(key)
                : comparer!.GetHashCode(key);
        var largeHash = hasher.Hash((ulong)systemHashCode);
        h1 = (uint)(largeHash >> 32);
        h2 = (byte)(largeHash & 0b0111_1111);
        // h1 = (uint)systemHashCode;
        // h2 = (byte)(systemHashCode & 0b0111_1111);
    }

    #endregion

    #region Growth

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int MakeGrowth(int size)
    {
        if (size == 0) return 0;
        if (size <= 8) return size - 1;
        return size / 8 * 7;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ShouldGrow(int count, int growthCount) => count >= growthCount;

    #endregion
}
