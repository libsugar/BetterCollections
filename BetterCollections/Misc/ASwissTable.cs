using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using BetterCollections.Buffers;
#if NET7_0_OR_GREATER
using System.Runtime.Intrinsics;
#endif
using BetterCollections.Memories;

namespace BetterCollections.Misc;

// reference https://faultlore.com/blah/hashbrown-tldr/
// reference https://github.com/rust-lang/hashbrown

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

    #region CtrlPos

    protected record struct CtrlPos(uint group, uint offset);

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
        => LoadGroup((byte*)Unsafe.AsPointer(ref Unsafe.AsRef(in ptr)), out output);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static unsafe void LoadGroup<V>(byte* ptr, out V output)
    {
        Unsafe.SkipInit(out output);
#if NET7_0_OR_GREATER
#if NET8_0_OR_GREATER
        if (Vector512.IsHardwareAccelerated)
        {
            Unsafe.As<V, Vector512<byte>>(ref output) = Vector512.Load(ptr);
            return;
        }
#endif
        if (Vector256.IsHardwareAccelerated)
        {
            Unsafe.As<V, Vector256<byte>>(ref output) = Vector256.Load(ptr);
            return;
        }
        if (Vector128.IsHardwareAccelerated)
        {
            Unsafe.As<V, Vector128<byte>>(ref output) = Vector128.Load(ptr);
            return;
        }
        if (Vector64.IsHardwareAccelerated)
        {
            Unsafe.As<V, Vector64<byte>>(ref output) = Vector64.Load(ptr);
            return;
        }
#endif
        Unsafe.As<V, ulong>(ref output) = Unsafe.ReadUnaligned<ulong>(ptr);
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

    #region FullBucketsIndicesSpanEnumerator

    protected ref struct FullBucketsIndicesSpanEnumerator<V>(Span<byte> ctrl)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FullBucketsIndicesSpanEnumerator<V> GetEnumerator() => this;

        private readonly Span<byte> ctrl = ctrl;
        private uint index = 0;
        private MatchBits match = LoadMatch(ctrl, 0);

        public uint Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MatchBits LoadMatch() => LoadMatch(ctrl, index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static MatchBits LoadMatch(Span<byte> ctrl, uint index)
        {
            LoadGroup<V>(in ctrl[(int)index], out var group);
            return MatchValue(ref group);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            for (;;)
            {
                if (match)
                {
                    Current = index + match.Offset;
                    match = match.Next;
                    return true;
                }
                if (index >= ctrl.Length) return false;
                index += CtrlGroupSize;
                if (index >= ctrl.Length) return false;
                LoadMatch();
            }
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
    private int version;

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

    protected int Version => version;

    protected Span<byte> Ctrl
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => size == 0 ? default : ctrl.AsSpan(0, (int)(size + CtrlGroupSize));
    }
    protected Span<T> Slots
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => size == 0 ? default : slots.AsSpan(0, (int)size);
    }

    protected byte[] BaseUnsafeGetCtrl() => ctrl;
    protected T[] BaseUnsafeGetSlot() => slots;

    #endregion

    #region Ctor

    protected ASwissTable(ArrayPoolFactory poolFactory, EH eh, int cap)
    {
        if (cap < 0) throw new ArgumentOutOfRangeException(nameof(cap), "capacity must > 0");
        // ReSharper disable once VirtualMemberCallInConstructor
        HandleMustReturn(poolFactory.MustReturn);
        count = 0;
        this.eh = eh;
        poolCtrl = poolFactory.GetMayUninitialized<byte>();
        poolSlot = poolFactory.Get<T>();
        var groupSize = CtrlGroupSize;
        size = cap == 0 ? 0 : ((uint)cap).CeilBinary(groupSize);
        ctrl = poolCtrl.Rent((int)(size == 0 ? 0 : size + groupSize));
        slots = poolSlot.Rent((int)size);
        Ctrl.Fill(SlotIsEmpty);
    }

    /// <summary>
    /// Hint: Virtual Member Call In <b>Constructor</b>
    /// <para>Data may not be available when this method is called</para>
    /// </summary>
    protected virtual void HandleMustReturn(bool mustReturn)
    {
        if (!mustReturn) GC.SuppressFinalize(this);
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

    protected bool TryFind<K, KEH>(in K key, in KEH keh, out uint slot_index) where KEH : IEqHashKey<T, K, EH>
    {
        Unsafe.SkipInit(out slot_index);
        if (size == 0) return false;

        var hash = keh.CalcHash(in eh, in key);
        var h1 = GetH1(hash);
        var h2 = GetH2(hash);

#if NET7_0_OR_GREATER
#if NET8_0_OR_GREATER
        if (Vector512.IsHardwareAccelerated)
            return TryFind<Vector512<byte>, K, KEH>(in key, in keh, h1, h2, out slot_index);
#endif
        if (Vector256.IsHardwareAccelerated)
            return TryFind<Vector256<byte>, K, KEH>(in key, in keh, h1, h2, out slot_index);
        if (Vector128.IsHardwareAccelerated)
            return TryFind<Vector128<byte>, K, KEH>(in key, in keh, h1, h2, out slot_index);
        if (Vector64.IsHardwareAccelerated)
            return TryFind<Vector64<byte>, K, KEH>(in key, in keh, h1, h2, out slot_index);
#endif
        return TryFind<ulong, K, KEH>(in key, in keh, h1, h2, out slot_index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryFind<V, K, KEH>(in K key, in KEH keh, uint h1, byte h2, out uint slot_index)
        where KEH : IEqHashKey<T, K, EH>
    {
        Unsafe.SkipInit(out slot_index);
        var size = this.size;
        if (size == 0 || count == 0) return false;

        var ctrl_bytes = Ctrl;
        var slots = Slots;

        for (var prop = H1StartProbe(h1);; prop.MoveNext(size))
        {
            LoadGroup<V>(in ctrl_bytes[(int)prop.pos], out var group);
            foreach (var offset in MatchH2(ref group, h2))
            {
                var index = ModGetBucket(prop.pos + offset);
                ref var slot = ref slots[(int)index];
                if (keh.IsEq(eh, in key, in slot))
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

    protected bool TryInsert(in T value, InsertBehavior behavior)
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
        if (size == 0 || NeedGrow()) goto grow;
        var r = TryGetInsertSlot<V>(in value, h1, h2, behavior, out var insert_slot, out var ctrl_pos);
        switch (r)
        {
            case SlotResult.Fail:
                return false;
            case SlotResult.Got:
                goto do_insert;
            case SlotResult.NeedGrow:
                goto grow;
            default:
                throw new ArgumentOutOfRangeException(null,
                    $"Internal control flow error at {nameof(TryInsert)}<{nameof(V)}>.{nameof(r)}");
        }

        grow:
        Grow();

        // ReSharper disable once RedundantAssignment
        var success = TryGetInsertSlot<V>(h1, out insert_slot, out ctrl_pos);
        Debug.Assert(success);

        do_insert:
        slots[insert_slot] = value;
        WriteCtrl(ctrl_pos, h2);
        count++;
        version = unchecked(version + 1);
        return true;
    }

    private enum SlotResult : byte
    {
        Fail,
        Got,
        NeedGrow,
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private SlotResult TryGetInsertSlot<V>(in T value, uint h1, byte h2, InsertBehavior behavior,
        out uint insert_slot, out CtrlPos group_pos)
    {
        Unsafe.SkipInit(out insert_slot);
        Unsafe.SkipInit(out group_pos);
        var size = this.size;
        if (size == 0) return SlotResult.NeedGrow;

        var ctrl_bytes = Ctrl;
        var slots = Slots;

        for (var prop = H1StartProbe(h1);; prop.MoveNext(size))
        {
            LoadGroup<V>(in ctrl_bytes[(int)prop.pos], out var group);

            {
                foreach (var offset in MatchH2(ref group, h2))
                {
                    var index = ModGetBucket(prop.pos + offset);
                    ref var slot = ref slots[(int)index];
                    if (eh.IsEq(in slot, in value))
                    {
                        if (behavior is not InsertBehavior.OverwriteIfExisting) return SlotResult.Fail;
                        insert_slot = index;
                        group_pos = new(prop.pos, offset);
                        return SlotResult.Got;
                    }
                }
            }

            {
                if (TryFindInsertSlotInGroup(ref group, in prop, out insert_slot, out var offset))
                {
                    group_pos = new(prop.pos, offset);
                    return SlotResult.Got;
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetInsertSlot<V>(uint h1, out uint insert_slot, out CtrlPos ctrl_pos)
    {
        Unsafe.SkipInit(out insert_slot);
        Unsafe.SkipInit(out ctrl_pos);
        var size = this.size;
        if (size == 0) return false;

        var ctrl_bytes = Ctrl;

        for (var prop = H1StartProbe(h1);; prop.MoveNext(size))
        {
            LoadGroup<V>(in ctrl_bytes[(int)prop.pos], out var group);

            if (TryFindInsertSlotInGroup(ref group, in prop, out insert_slot, out var offset))
            {
                ctrl_pos = new(prop.pos, offset);
                return true;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryFindInsertSlotInGroup<V>(ref V group, in ProbeSeq prop, out uint insert_slot, out uint offset)
    {
        Unsafe.SkipInit(out insert_slot);
        Unsafe.SkipInit(out offset);
        var match = MatchEmptyOrDelete(ref group);
        if (match)
        {
            offset = match.Offset;
            insert_slot = ModGetBucket(prop.pos + offset);
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteCtrl(CtrlPos ctrl_pos, byte h2)
    {
        var size = this.size;
        var ctrl_bytes = Ctrl;
        var pos = ctrl_pos.group + ctrl_pos.offset;
        var mod_pos = ModGetBucket(pos);
        ctrl_bytes[(int)mod_pos] = h2;
        if (mod_pos != pos)
        {
            ctrl_bytes[(int)pos] = h2;
        }
        if (ctrl_pos.group < CtrlGroupSize)
        {
            ctrl_bytes[(int)(size + ctrl_pos.offset)] = h2;
        }
    }

    #endregion

    #region Grow

    protected bool NeedGrow() => count + CtrlGroupSize / 2 > size;

    protected void Grow()
    {
        if (size == 0) Grow(CtrlGroupSize, true);
        // size is a power of 2
        else Grow(size << 1, false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Grow(uint new_size, bool init)
    {
        var old_size = size;
        var old_ctrl = ctrl;
        var old_slots = slots;

        var groupSize = CtrlGroupSize;
        var new_ctrl = poolCtrl.Rent((int)(new_size + groupSize));
        var new_slots = poolSlot.Rent((int)new_size);

        new_ctrl.AsSpan(0, (int)(new_size + groupSize)).Fill(SlotIsEmpty);

        size = new_size;
        ctrl = new_ctrl;
        slots = new_slots;
        version++;

        if (!init)
        {
            try
            {
                ReInsert(old_ctrl, old_slots, old_size);
            }
            catch
            {
                size = old_size;
                ctrl = old_ctrl;
                slots = old_slots;

                poolCtrl.Return(new_ctrl);
                poolSlot.Return(new_slots, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
            }
        }

        poolCtrl.Return(old_ctrl);
        poolSlot.Return(old_slots, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
    }

    private void ReInsert(byte[] old_ctrl, T[] old_slots, uint old_size)
    {
#if NET7_0_OR_GREATER
#if NET8_0_OR_GREATER
        if (Vector512.IsHardwareAccelerated)
        {
            ReInsert<Vector512<byte>>(old_ctrl, old_slots, old_size);
            return;
        }
#endif
        if (Vector256.IsHardwareAccelerated)
        {
            ReInsert<Vector256<byte>>(old_ctrl, old_slots, old_size);
            return;
        }
        if (Vector128.IsHardwareAccelerated)
        {
            ReInsert<Vector128<byte>>(old_ctrl, old_slots, old_size);
            return;
        }
        if (Vector64.IsHardwareAccelerated)
        {
            ReInsert<Vector64<byte>>(old_ctrl, old_slots, old_size);
            return;
        }
#endif
        {
            ReInsert<ulong>(old_ctrl, old_slots, old_size);
            return;
        }
    }

    private void ReInsert<V>(byte[] old_ctrl, T[] old_slots, uint old_size)
    {
        var slots = Slots;
        foreach (var old_index in new FullBucketsIndicesSpanEnumerator<V>(old_ctrl.AsSpan(0, (int)old_size)))
        {
            ref var old_slot = ref old_slots[old_index];
            
            var hash = eh.CalcHash(in old_slot);
            var h1 = GetH1(hash);
            var h2 = GetH2(hash);

            // ReSharper disable once RedundantAssignment
            var success = TryGetInsertSlot<V>(h1, out var insert_slot, out var ctrl_pos);
            Debug.Assert(success);

            slots[(int)insert_slot] = old_slot;
            WriteCtrl(ctrl_pos, h2);
            version++;
        }
    }

    #endregion

    #region Clear

    public void Clear()
    {
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
