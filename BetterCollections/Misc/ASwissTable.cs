using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BetterCollections.Buffers;
#if NET7_0_OR_GREATER
using System.Runtime.Intrinsics;
#endif

namespace BetterCollections.Misc;

public abstract partial class ASwissTable
{
    #region Consts

    protected const byte SlotIsEmpty = 0b1000_0000;
    protected const byte SlotIsDeleted = 0b1111_1110;
    protected const byte SlotHasValueFlag = 0b1000_0000;
    protected const byte SlotValueMask = 0b0111_1111;

    #endregion

    #region H1 H2

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static uint GetH1(int hashCode) => (uint)hashCode & ~(uint)SlotValueMask;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static byte GetH2(int hashCode) => (byte)((uint)hashCode & SlotValueMask);

    #endregion

    #region CtrlGroupType

    public static Type CtrlGroupType
    {
        get
        {
#if NETSTANDARD || NET6_0
            return typeof(ulong);
#else
#if NET8_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated) return typeof(Vector512<byte>);
#endif
            if (Vector256.IsHardwareAccelerated) return typeof(Vector256<byte>);
            return typeof(Vector128<byte>);
#endif
        }
    }

    #endregion

    #region CtrlGroupSize

    /// <summary>
    /// Group size that matches simd size, it will only be one of 16 32 64
    /// </summary>
    protected static uint CtrlGroupSize
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get =>
#if NETSTANDARD || NET6_0
            8u
#else
#if NET8_0_OR_GREATER
            Vector512.IsHardwareAccelerated ? 64u :
#endif
            Vector256.IsHardwareAccelerated ? 32u : 16u
#endif
        ;
    }

    #endregion

    #region GetArrayPoolCtrl

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static object GetArrayPoolCtrl(ArrayPoolFactory poolFactory)
    {
        return
#if NETSTANDARD || NET6_0
            poolFactory.Get<ulong>();
#else
#if NET8_0_OR_GREATER
            Vector512.IsHardwareAccelerated ? poolFactory.Get<Vector512<byte>>() :
#endif
            Vector256.IsHardwareAccelerated ? poolFactory.Get<Vector256<byte>>() : poolFactory.Get<Vector128<byte>>();
#endif
    }

    #endregion

    #region RentCtrl

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static Array RentCtrl(object poolCtrl, int groups)
    {
        return
#if NETSTANDARD || NET6_0
            ((ArrayPool<ulong>)poolCtrl).Rent(groups);
#else
#if NET8_0_OR_GREATER
            Vector512.IsHardwareAccelerated ? ((ArrayPool<Vector512<byte>>)poolCtrl).Rent(groups) :
#endif
            Vector256.IsHardwareAccelerated
                ? ((ArrayPool<Vector256<byte>>)poolCtrl).Rent(groups)
                : ((ArrayPool<Vector128<byte>>)poolCtrl).Rent(groups);
#endif
    }

    #endregion

    #region ReturnCtrl

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static void ReturnCtrl(object poolCtrl, Array ctrl)
    {
#if NETSTANDARD || NET6_0
        ((ArrayPool<ulong>)poolCtrl).Return((ulong[])ctrl);
#else
#if NET8_0_OR_GREATER
        if (Vector512.IsHardwareAccelerated)
        {
            ((ArrayPool<Vector512<byte>>)poolCtrl).Return((Vector512<byte>[])ctrl);
            return;
        }
#endif
        if (Vector256.IsHardwareAccelerated)
        {
            ((ArrayPool<Vector256<byte>>)poolCtrl).Return((Vector256<byte>[])ctrl);
            return;
        }
        ((ArrayPool<Vector128<byte>>)poolCtrl).Return((Vector128<byte>[])ctrl);
#endif
    }

    #endregion

    #region CtrlAsBytes

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static Span<byte> CtrlAsBytes(Array ctrl, int size)
    {
        Span<byte> span;
#if NETSTANDARD || NET6_0
        span = MemoryMarshal.Cast<ulong, byte>(((ulong[])ctrl).AsSpan());
#else
#if NET8_0_OR_GREATER
        if (Vector512.IsHardwareAccelerated)
        {
            span = MemoryMarshal.Cast<Vector512<byte>, byte>(((Vector512<byte>[])ctrl).AsSpan());
            goto r;
        }
#endif
        if (Vector256.IsHardwareAccelerated)
        {
            span = MemoryMarshal.Cast<Vector256<byte>, byte>(((Vector256<byte>[])ctrl).AsSpan());
            goto r;
        }
        span = MemoryMarshal.Cast<Vector128<byte>, byte>(((Vector128<byte>[])ctrl).AsSpan());
#endif
#pragma warning disable CS0164
        r:
#pragma warning restore CS0164
        return span[..size];
    }

    #endregion

    #region TryH2GetSlot

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static bool TryH2GetSlot(uint size, Span<byte> ctrl_bytes, uint start, byte h2, out uint slot)
    {
        Unsafe.SkipInit(out slot);
        if (size == 0) return false;
#if NETSTANDARD || NET6_0
        Span<byte> bytes = stackalloc byte[8];
        bytes.Fill(h2);
        var ul = MemoryMarshal.Cast<byte, ulong>(bytes)[0];
        return TryH2GetSlot(ctrl_bytes, start, ul, out slot);
#else
#if NET8_0_OR_GREATER
        if (Vector512.IsHardwareAccelerated)
            return TryH2GetSlot(ctrl_bytes, start, Vector512.Create(h2), out slot);
#endif
        if (Vector256.IsHardwareAccelerated)
            return TryH2GetSlot(ctrl_bytes, start, Vector256.Create(h2), out slot);
        return TryH2GetSlot(ctrl_bytes, start, Vector128.Create(h2), out slot);
#endif
    }

    #endregion
}

public abstract class ASwissTable<T, EH> : ASwissTable, IDisposable
    where EH : IEqHash<T>
{
    #region Fields

    /// <summary>
    /// Control character, the content can be
    /// <see cref="ASwissTable.SlotIsEmpty"/>, <see cref="ASwissTable.SlotIsDeleted"/>,
    /// SlotHasValue: first bit is 1 and the other bits are hash
    /// </summary>
    private Array ctrl;
    /// <summary>
    /// The slot to store the value and hash
    /// </summary>
    private T[] slots;
    /// <summary>
    /// The actual array size used, because the array is allocated from the pool, the array size may be larger than the used size
    /// <para><b>Must be power of two</b></para>
    /// </summary>
    private uint size;
    /// <summary>
    /// groups - 1, for fast modulus calculations
    /// </summary>
    private uint groupsMinusOne;
    /// <summary>
    /// How many elements are stored
    /// </summary>
    private int count;

    protected readonly object poolCtrl;
    protected readonly ArrayPool<T> poolSlot;
    protected readonly EH eh;

    #endregion

    #region Getters

    /// <summary>
    /// How many elements are stored
    /// </summary>
    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => count;
    }

    protected uint Groups
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => groupsMinusOne + 1;
    }

    protected Span<byte> Ctrl
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => CtrlAsBytes(ctrl, (int)size);
    }
    protected Span<T> Slots
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => slots.AsSpan(0, (int)size);
    }

    public Array UnsafeGetCtrl() => ctrl;
    public T[] UnsafeGetSlot() => slots;

    #endregion

    #region Ctor

    protected ASwissTable(ArrayPoolFactory poolFactory, EH eh, int cap)
    {
        this.eh = eh;
        poolCtrl = GetArrayPoolCtrl(poolFactory);
        poolSlot = poolFactory.Get<T>();
        if (cap < 0) throw new ArgumentOutOfRangeException(nameof(cap), "capacity must > 0");
        count = 0;
        var groupSize = CtrlGroupSize;
        size = cap == 0 ? 0 : ((uint)cap).CeilBinary(groupSize);
        var groups = size / groupSize;
        groupsMinusOne = groups - 1;
        ctrl = RentCtrl(poolCtrl, (int)size);
        slots = poolSlot.Rent((int)size);
        Ctrl.Fill(SlotIsEmpty);
    }

    #endregion

    #region GetCtrlGroup

    /// <summary>
    /// Same to <c>h1 % groups</c> because size and groups must be power of two
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected uint GetCtrlGroup(uint h1) => h1 & groupsMinusOne;

    #endregion

    #region TryInsert

    protected bool TryInsert(T value, InsertBehavior behavior)
    {
        var hash = eh.CalcHash(in value);
        var h1 = GetH1(hash);
        var h2 = GetH2(hash);

        if (size == 0) goto do_add;

        var start_group = GetCtrlGroup(h1);
        if (TryH2GetSlot(size, Ctrl, start_group, h2, out var slot_index))
        {
            throw new NotImplementedException();
        }

        do_add:
        throw new NotImplementedException();
    }

    #endregion

    #region Clear

    public void Clear()
    {
        throw new System.NotImplementedException();
    }

    #endregion

    #region Dispose

    protected virtual void Dispose(bool disposing)
    {
        if (ctrl != null!)
        {
            ReturnCtrl(poolCtrl, ctrl);
            ctrl = null!;
        }
        if (slots != null!)
        {
            poolSlot.Return(slots, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
            slots = null!;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~ASwissTable() => Dispose(false);

    #endregion
}
