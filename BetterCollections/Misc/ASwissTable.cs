using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using BetterCollections.Buffers;
#if NET7_0_OR_GREATER
using System.Runtime.Intrinsics;
#endif

namespace BetterCollections.Misc;

public abstract partial class ASwissTable
{
    #region Consts

    protected const byte SlotIsEmpty = 0b1111_1111;
    protected const byte SlotIsDeleted = 0b1000_0000;
    protected const byte SlotValueMask = 0b0111_1111;

    #endregion

    #region H1 H2

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static uint GetH1(int hashCode) => (uint)hashCode & ~(uint)SlotValueMask;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static byte GetH2(int hashCode) => (byte)((uint)hashCode & SlotValueMask);

    #endregion

    #region MatchBits

    protected readonly partial struct MatchBits(ulong bits, bool soft = false) : IEquatable<MatchBits>
    {
        #region Consts

        private const uint SoftStride = 8;

        #endregion

        public readonly ulong bits = bits;
        public readonly bool soft = soft;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator true(MatchBits match) => match.bits != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator false(MatchBits match) => match.bits == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator bool(MatchBits match) => match.bits != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ulong(MatchBits match) => match.bits;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator string(MatchBits match) => match.ToString();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MatchBitsIter GetEnumerator() => new(this);

        /// <summary>
        /// Are there any bits set
        /// </summary>
        public bool Has
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this;
        }

        /// <summary>
        /// Trailing Zero Count
        /// </summary>
        public uint Offset
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // ReSharper disable once JoinDeclarationAndInitializer
                uint r;
#if NETSTANDARD || NET6_0
                r = Utils.TrailingZeroCount(bits);
#else
                r = (uint)ulong.TrailingZeroCount(bits);
#endif
                if (!soft) return r;
                return r / SoftStride;
            }
        }

        /// <summary>
        /// Remove lowest bit
        /// </summary>
        public MatchBits Next
        {
            // https://graphics.stanford.edu/~seander/bithacks.html#CountBitsSetKernighan
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(bits & (bits - 1), soft);
        }

        public struct MatchBitsIter(MatchBits match)
        {
            private MatchBits match = match;

            public uint Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get;
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private set;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (!match) return false;
                Current = match.Offset;
                match = match.Next;
                return true;
            }
        }

        #region ToString

        private string BaseToString() => Convert.ToString((long)bits, 2).PadLeft(64, '0');

#if NET8_0_OR_GREATER
        [GeneratedRegex(".{8}(?!$)")]
        private static partial Regex SplitBytes();

        public override string ToString() =>
            SplitBytes().Replace(BaseToString(), "$0_");
#else
        public override string ToString() =>
            Regex.Replace(BaseToString(), ".{8}(?!$)", "$0_");
#endif

        #endregion

        #region Equals

        public bool Equals(MatchBits other) => bits == other.bits;

        public override bool Equals(object? obj) => obj is MatchBits other && Equals(other);

        public override int GetHashCode() => bits.GetHashCode();

        public static bool operator ==(MatchBits left, MatchBits right) => left.Equals(right);

        public static bool operator !=(MatchBits left, MatchBits right) => !left.Equals(right);

        #endregion
    }

    #endregion

    #region CtrlGroupType

    public static Type CtrlGroupType
    {
        get
        {
#if NET7_0_OR_GREATER
#if NET8_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated) return typeof(Vector512<byte>);
#endif
            if (Vector256.IsHardwareAccelerated) return typeof(Vector256<byte>);
            if (Vector128.IsHardwareAccelerated) return typeof(Vector128<byte>);
            if (Vector64.IsHardwareAccelerated) return typeof(Vector64<byte>);
#endif
            return typeof(ulong);
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
        get
        {
#if NET7_0_OR_GREATER
#if NET8_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated) return 64u;
#endif
            if (Vector256.IsHardwareAccelerated) return 32u;
            if (Vector128.IsHardwareAccelerated) return 16u;
#endif
            return 8u;
        }
    }

    #endregion

    #region ModGetBucket

    /// <summary>
    /// Same to <c>v % size</c> because size must be power of two
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static uint ModGetBucket(uint v, uint size) => v & (size - 1);

    #endregion

    #region ProbeSeq

    protected struct ProbeSeq(uint pos, uint stride = 0)
    {
        public uint pos = pos;
        public uint stride = stride;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MoveNext(uint size)
        {
            Debug.Assert(stride < size);

            stride += CtrlGroupSize;
            pos += stride;
            pos = ModGetBucket(pos, size);
        }
    }

    #endregion

    #region LoadGroup

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static unsafe void LoadGroup<V>(in byte ptr, out V output)
    {
        Unsafe.SkipInit(out output);
#if NET7_0_OR_GREATER
#if NET8_0_OR_GREATER
        if (Vector512.IsHardwareAccelerated)
        {
            Unsafe.As<V, Vector512<byte>>(ref output) = Vector512.Load((byte*)ptr);
            return;
        }
#endif
        if (Vector256.IsHardwareAccelerated)
        {
            Unsafe.As<V, Vector256<byte>>(ref output) = Vector256.Load((byte*)ptr);
            return;
        }
        if (Vector128.IsHardwareAccelerated)
        {
            Unsafe.As<V, Vector128<byte>>(ref output) = Vector128.Load((byte*)ptr);
            return;
        }
        if (Vector64.IsHardwareAccelerated)
        {
            Unsafe.As<V, Vector64<byte>>(ref output) = Vector64.Load((byte*)ptr);
            return;
        }
#endif
        Unsafe.As<V, ulong>(ref output) = Unsafe.ReadUnaligned<ulong>((byte*)ptr);
    }

    #endregion

    #region MatchH2

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static MatchBits MatchH2<V>(ref V group, byte h2)
    {
#if NET7_0_OR_GREATER
#if NET8_0_OR_GREATER
        if (Vector512.IsHardwareAccelerated)
        {
            return MatchH2(Unsafe.As<V, Vector512<byte>>(ref group), Vector512.Create(h2));
        }
#endif
        if (Vector256.IsHardwareAccelerated)
        {
            return MatchH2(Unsafe.As<V, Vector256<byte>>(ref group), Vector256.Create(h2));
        }
        if (Vector128.IsHardwareAccelerated)
        {
            return MatchH2(Unsafe.As<V, Vector128<byte>>(ref group), Vector128.Create(h2));
        }
        if (Vector64.IsHardwareAccelerated)
        {
            return MatchH2(Unsafe.As<V, Vector64<byte>>(ref group), Vector64.Create(h2));
        }
#endif
        {
            return MatchH2(Unsafe.As<V, ulong>(ref group), Utils.CreateULong(h2));
        }
    }

    #endregion

    #region MatchEmpty

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static MatchBits MatchEmpty<V>(ref V group)
    {
#if NET7_0_OR_GREATER
#if NET8_0_OR_GREATER
        if (Vector512.IsHardwareAccelerated)
        {
            return MatchEmpty(Unsafe.As<V, Vector512<byte>>(ref group));
        }
#endif
        if (Vector256.IsHardwareAccelerated)
        {
            return MatchEmpty(Unsafe.As<V, Vector256<byte>>(ref group));
        }
        if (Vector128.IsHardwareAccelerated)
        {
            return MatchEmpty(Unsafe.As<V, Vector128<byte>>(ref group));
        }
        if (Vector64.IsHardwareAccelerated)
        {
            return MatchEmpty(Unsafe.As<V, Vector64<byte>>(ref group));
        }
#endif
        {
            return MatchEmpty(Unsafe.As<V, ulong>(ref group));
        }
    }

    #endregion

    #region MatchEmptyOrDelete

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static MatchBits MatchEmptyOrDelete<V>(ref V group)
    {
#if NET7_0_OR_GREATER
#if NET8_0_OR_GREATER
        if (Vector512.IsHardwareAccelerated)
        {
            return MatchEmptyOrDelete(Unsafe.As<V, Vector512<byte>>(ref group));
        }
#endif
        if (Vector256.IsHardwareAccelerated)
        {
            return MatchEmptyOrDelete(Unsafe.As<V, Vector256<byte>>(ref group));
        }
        if (Vector128.IsHardwareAccelerated)
        {
            return MatchEmptyOrDelete(Unsafe.As<V, Vector128<byte>>(ref group));
        }
        if (Vector64.IsHardwareAccelerated)
        {
            return MatchEmptyOrDelete(Unsafe.As<V, Vector64<byte>>(ref group));
        }
#endif
        {
            return MatchEmptyOrDelete(Unsafe.As<V, ulong>(ref group));
        }
    }

    #endregion

    #region MatchEmptyOrDelete

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static MatchBits MatchValue<V>(ref V group)
    {
#if NET7_0_OR_GREATER
#if NET8_0_OR_GREATER
        if (Vector512.IsHardwareAccelerated)
        {
            return MatchValue(Unsafe.As<V, Vector512<byte>>(ref group));
        }
#endif
        if (Vector256.IsHardwareAccelerated)
        {
            return MatchValue(Unsafe.As<V, Vector256<byte>>(ref group));
        }
        if (Vector128.IsHardwareAccelerated)
        {
            return MatchValue(Unsafe.As<V, Vector128<byte>>(ref group));
        }
        if (Vector64.IsHardwareAccelerated)
        {
            return MatchValue(Unsafe.As<V, Vector64<byte>>(ref group));
        }
#endif
        {
            return MatchValue(Unsafe.As<V, ulong>(ref group));
        }
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
    private byte[] ctrl;
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
    /// How many elements are stored
    /// </summary>
    private int count;

    protected readonly ArrayPool<byte> poolCtrl;
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

    protected Span<byte> Ctrl
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ctrl.AsSpan(0, (int)(size + CtrlGroupSize));
    }
    protected Span<T> Slots
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => slots.AsSpan(0, (int)size);
    }

    protected byte[] BaseUnsafeGetCtrl() => ctrl;
    protected T[] BaseUnsafeGetSlot() => slots;

    #endregion

    #region Ctor

    protected ASwissTable(ArrayPoolFactory poolFactory, EH eh, int cap)
    {
        this.eh = eh;
        poolCtrl = poolFactory.Get<byte>();
        poolSlot = poolFactory.Get<T>();
        if (cap < 0) throw new ArgumentOutOfRangeException(nameof(cap), "capacity must > 0");
        count = 0;
        var groupSize = CtrlGroupSize;
        size = cap == 0 ? 0 : ((uint)cap).CeilBinary(groupSize);
        ctrl = poolCtrl.Rent((int)(size + groupSize));
        slots = poolSlot.Rent((int)size);
        Ctrl.Fill(SlotIsEmpty);
    }

    #endregion

    #region ModGetBucket

    /// <inheritdoc cref="ASwissTable.ModGetBucket(uint,uint)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected uint ModGetBucket(uint v) => ModGetBucket(v, size);

    #endregion

    #region ProbeSeq

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ProbeSeq H1StartProbe(uint h1) => new(ModGetBucket(h1));

    #endregion

    #region TryFind

    protected bool TryFind(T value, out uint slot_index)
    {
        Unsafe.SkipInit(out slot_index);
        if (size == 0) return false;

        var hash = eh.CalcHash(in value);
        var h1 = GetH1(hash);
        var h2 = GetH2(hash);

#if NET7_0_OR_GREATER
#if NET8_0_OR_GREATER
        if (Vector512.IsHardwareAccelerated)
            return TryFind<Vector512<byte>>(in value, h1, h2, out slot_index);
#endif
        if (Vector256.IsHardwareAccelerated)
            return TryFind<Vector256<byte>>(in value, h1, h2, out slot_index);
        if (Vector128.IsHardwareAccelerated)
            return TryFind<Vector128<byte>>(in value, h1, h2, out slot_index);
        if (Vector64.IsHardwareAccelerated)
            return TryFind<Vector64<byte>>(in value, h1, h2, out slot_index);
#endif
        return TryFind<ulong>(in value, h1, h2, out slot_index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryFind<V>(in T value, uint h1, byte h2, out uint slot_index)
    {
        Unsafe.SkipInit(out slot_index);
        var size = this.size;
        if (size == 0) return false;

        var ctrl_bytes = Ctrl;
        var slots = Slots;

        for (var prop = H1StartProbe(h1);; prop.MoveNext(size))
        {
            LoadGroup<V>(in ctrl_bytes[(int)prop.pos], out var group);
            foreach (var offset in MatchH2(ref group, h2))
            {
                var index = ModGetBucket(prop.pos + offset);
                ref var slot = ref slots[(int)index];
                if (eh.IsEq(in slot, in value))
                {
                    slot_index = index;
                    return true;
                }
            }
            if (MatchEmpty(ref group)) return false;
        }
    }

    #endregion

    #region TryInsert

    protected bool TryInsert(T value, InsertBehavior behavior)
    {
        var hash = eh.CalcHash(in value);
        var h1 = GetH1(hash);
        var h2 = GetH2(hash);

#if NET7_0_OR_GREATER
#if NET8_0_OR_GREATER
        if (Vector512.IsHardwareAccelerated)
            return TryInsert<Vector512<byte>>(in value, h1, h2, behavior);
#endif
        if (Vector256.IsHardwareAccelerated)
            return TryInsert<Vector256<byte>>(in value, h1, h2, behavior);
        if (Vector128.IsHardwareAccelerated)
            return TryInsert<Vector128<byte>>(in value, h1, h2, behavior);
        if (Vector64.IsHardwareAccelerated)
            return TryInsert<Vector64<byte>>(in value, h1, h2, behavior);
#endif
        return TryInsert<ulong>(in value, h1, h2, behavior);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryInsert<V>(in T value, uint h1, byte h2, InsertBehavior behavior)
    {
        if (TryFind<V>(in value, h1, h2, out var slot_index))
        {
            if (behavior is not InsertBehavior.OverwriteIfExisting) return false;
            slots[slot_index] = value;
            return true;
        }
        throw new NotImplementedException();
    }

    #endregion

    #region Grow

    protected void Grow()
    {
        if (size == 0) Grow(CtrlGroupSize * 8, true);
        // size is a power of 2
        else Grow(size << 1, false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Grow(uint new_size, bool init)
    {
        var old_ctrl = ctrl;
        var old_slots = slots;

        var groupSize = CtrlGroupSize;
        var new_ctrl = poolCtrl.Rent((int)(new_size + groupSize));
        var new_slots = poolSlot.Rent((int)new_size);

        new_ctrl.AsSpan(0, (int)(new_size + groupSize)).Fill(SlotIsEmpty);

        if (!init)
        {
            throw new NotImplementedException("todo");
        }

        size = new_size;
        ctrl = new_ctrl;
        slots = new_slots;
        poolCtrl.Return(old_ctrl);
        poolSlot.Return(old_slots, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
    }

    #endregion

    #region Clear

    public void Clear()
    {
        // todo narrow
        Ctrl.Fill(SlotIsEmpty);
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) Slots.Clear();
    }

    #endregion

    #region Dispose

    protected virtual void Dispose(bool disposing)
    {
        if (ctrl != null!)
        {
            poolCtrl.Return(ctrl);
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
