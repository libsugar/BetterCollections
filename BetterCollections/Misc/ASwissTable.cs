using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using BetterCollections.Buffers;
using BetterCollections.Cryptography;
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

    protected const uint DefaultCapacity = 4;

    #endregion

    #region H1 H2

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static ulong GetH1(ulong hashCode) => hashCode >> 7;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static byte GetH2(ulong hashCode) => (byte)(hashCode & SlotValueMask);

    #endregion

    #region CtrlPos

    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected record struct CtrlPos(uint group, uint offset);

    #endregion

    #region MatchBits

    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        public override string ToString() => bits.ToBinaryString();

        #endregion

        #region Equals

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(MatchBits other) => bits == other.bits;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj) => obj is MatchBits other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => bits.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(MatchBits left, MatchBits right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(MatchBits left, MatchBits right) => !left.Equals(right);

        #endregion
    }

    #endregion

    #region CtrlGroupType

    public static Type CtrlGroupType
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    #region Cap / Growth

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static uint CeilCap(uint cap)
    {
        if (cap == 0) return 0;
        var groupSize = CtrlGroupSize;
#if NET7_0_OR_GREATER
        if (uint.IsPow2(cap)) return cap;
#else
        if (Utils.IsPow2(cap)) return cap;
#endif
        return cap switch
        {
            < 4 => 4,
            < 8 => 8,
            _ => cap.CeilBinary(groupSize)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static uint MakeGrowth(uint slots_size)
    {
        if (slots_size == 0) return 0;
        if (slots_size <= 8) return slots_size - 1;
        return slots_size / 8 * 7;
    }

    #endregion

    #region ModGetBucket

    /// <summary>
    /// Same to <c>v % size</c> because size must be power of two
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static uint ModGetBucket(uint v, uint size) => v & (size - 1);

    /// <summary>
    /// Same to <c>v % size</c> because size must be power of two
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static uint ModGetBucket(ulong v, uint size) => (uint)(v & (size - 1));

    #endregion

    #region ProbeSeq

    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected struct ProbeSeq(uint pos, uint stride = 0)
    {
        public uint pos = pos;
        public uint stride = stride;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MoveNext(uint slots_size)
        {
            Debug.Assert(stride < slots_size);

            stride += CtrlGroupSize;
            pos += stride;
            pos = ModGetBucket(pos, slots_size);
        }
    }

    #endregion

    #region LoadGroup

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static unsafe void LoadGroup<V>(in byte ptr, out V output)
        => LoadGroup((byte*)Unsafe.AsPointer(ref Unsafe.AsRef(in ptr)), out output);

    [MethodImpl(MethodImplOptions.AggressiveInlining | (MethodImplOptions)512)]
    protected static unsafe void LoadGroup<V>(byte* ptr, out V output)
    {
        Unsafe.SkipInit(out output);
#if NET7_0_OR_GREATER
#if NET8_0_OR_GREATER
        if (typeof(V) == typeof(Vector512<byte>))
        {
            Unsafe.As<V, Vector512<byte>>(ref output) = Vector512.Load(ptr);
            return;
        }
#endif
        if (typeof(V) == typeof(Vector256<byte>))
        {
            Unsafe.As<V, Vector256<byte>>(ref output) = Vector256.Load(ptr);
            return;
        }
        if (typeof(V) == typeof(Vector128<byte>))
        {
            Unsafe.As<V, Vector128<byte>>(ref output) = Vector128.Load(ptr);
            return;
        }
        if (typeof(V) == typeof(Vector64<byte>))
        {
            Unsafe.As<V, Vector64<byte>>(ref output) = Vector64.Load(ptr);
            return;
        }
#endif
        if (typeof(V) == typeof(ulong))
        {
            Unsafe.As<V, ulong>(ref output) = Unsafe.ReadUnaligned<ulong>(ptr);
            return;
        }
        throw new NotSupportedException();
    }

    #endregion

    #region MatchH2

    [MethodImpl(MethodImplOptions.AggressiveInlining | (MethodImplOptions)512)]
    protected static MatchBits MatchH2<V>(ref V group, byte h2)
    {
#if NET7_0_OR_GREATER
#if NET8_0_OR_GREATER
        if (typeof(V) == typeof(Vector512<byte>))
        {
            return new(MatchH2(in Unsafe.As<V, Vector512<byte>>(ref group), h2));
        }
#endif
        if (typeof(V) == typeof(Vector256<byte>))
        {
            return new(MatchH2(in Unsafe.As<V, Vector256<byte>>(ref group), h2));
        }
        if (typeof(V) == typeof(Vector128<byte>))
        {
            return new(MatchH2(in Unsafe.As<V, Vector128<byte>>(ref group), h2));
        }
        if (typeof(V) == typeof(Vector64<byte>))
        {
            return new(MatchH2(in Unsafe.As<V, Vector64<byte>>(ref group), h2));
        }
#endif
        if (typeof(V) == typeof(ulong))
        {
            return MatchH2(Unsafe.As<V, ulong>(ref group), Utils.CreateULong(h2));
        }
        throw new NotSupportedException();
    }

    #endregion

    #region MatchEmpty

    [MethodImpl(MethodImplOptions.AggressiveInlining | (MethodImplOptions)512)]
    protected static MatchBits MatchEmpty<V>(ref V group)
    {
#if NET7_0_OR_GREATER
#if NET8_0_OR_GREATER
        if (typeof(V) == typeof(Vector512<byte>))
        {
            return new(MatchEmpty(in Unsafe.As<V, Vector512<byte>>(ref group)));
        }
#endif
        if (typeof(V) == typeof(Vector256<byte>))
        {
            return new(MatchEmpty(in Unsafe.As<V, Vector256<byte>>(ref group)));
        }
        if (typeof(V) == typeof(Vector128<byte>))
        {
            return new(MatchEmpty(in Unsafe.As<V, Vector128<byte>>(ref group)));
        }
        if (typeof(V) == typeof(Vector64<byte>))
        {
            return new(MatchEmpty(in Unsafe.As<V, Vector64<byte>>(ref group)));
        }
#endif
        if (typeof(V) == typeof(ulong))
        {
            return MatchEmpty(Unsafe.As<V, ulong>(ref group));
        }
        throw new NotSupportedException();
    }

    #endregion

    #region MatchEmptyOrDelete

    [MethodImpl(MethodImplOptions.AggressiveInlining | (MethodImplOptions)512)]
    protected static MatchBits MatchEmptyOrDelete<V>(ref V group)
    {
#if NET7_0_OR_GREATER
#if NET8_0_OR_GREATER
        if (typeof(V) == typeof(Vector512<byte>))
        {
            return new(MatchEmptyOrDelete(in Unsafe.As<V, Vector512<byte>>(ref group)));
        }
#endif
        if (typeof(V) == typeof(Vector256<byte>))
        {
            return new(MatchEmptyOrDelete(in Unsafe.As<V, Vector256<byte>>(ref group)));
        }
        if (typeof(V) == typeof(Vector128<byte>))
        {
            return new(MatchEmptyOrDelete(in Unsafe.As<V, Vector128<byte>>(ref group)));
        }
        if (typeof(V) == typeof(Vector64<byte>))
        {
            return new(MatchEmptyOrDelete(in Unsafe.As<V, Vector64<byte>>(ref group)));
        }
#endif
        if (typeof(V) == typeof(ulong))
        {
            return MatchEmptyOrDelete(Unsafe.As<V, ulong>(ref group));
        }
        throw new NotSupportedException();
    }

    #endregion

    #region MatchEmptyOrDelete

    [MethodImpl(MethodImplOptions.AggressiveInlining | (MethodImplOptions)512)]
    protected static MatchBits MatchValue<V>(ref V group)
    {
#if NET7_0_OR_GREATER
#if NET8_0_OR_GREATER
        if (typeof(V) == typeof(Vector512<byte>))
        {
            return new(MatchValue(in Unsafe.As<V, Vector512<byte>>(ref group)));
        }
#endif
        if (typeof(V) == typeof(Vector256<byte>))
        {
            return new(MatchValue(in Unsafe.As<V, Vector256<byte>>(ref group)));
        }
        if (typeof(V) == typeof(Vector128<byte>))
        {
            return new(MatchValue(in Unsafe.As<V, Vector128<byte>>(ref group)));
        }
        if (typeof(V) == typeof(Vector64<byte>))
        {
            return new(MatchValue(in Unsafe.As<V, Vector64<byte>>(ref group)));
        }
#endif
        if (typeof(V) == typeof(ulong))
        {
            return MatchValue(Unsafe.As<V, ulong>(ref group));
        }
        throw new NotSupportedException();
    }

    #endregion

    #region FullBucketsIndicesSpanEnumerator

    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ref struct FullBucketsIndicesSpanEnumerator<V>(ReadOnlySpan<byte> ctrl, uint slots_size, uint group_size)
    {
        public FullBucketsIndicesSpanEnumerator<V> GetEnumerator() => this;

        private readonly ReadOnlySpan<byte> ctrl = ctrl;
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
        private static MatchBits LoadMatch(ReadOnlySpan<byte> ctrl, uint index)
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
                    var pos = index + match.Offset;
                    if (pos >= slots_size) return false;
                    Current = pos;
                    match = match.Next;
                    return true;
                }
                if (index >= slots_size) return false;
                index += group_size;
                if (index >= slots_size) return false;
                match = LoadMatch();
            }
        }
    }

    #endregion

    #region FullBucketsIndicesMemoryEnumerator

    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected struct FullBucketsIndicesMemoryEnumerator(ReadOnlyMemory<byte> ctrl, uint slots_size, uint group_size)
        : IEnumerable<uint>, IEnumerator<uint>
    {
        private uint index = 0;
        private MatchBits match = LoadMatch(ctrl.Span, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FullBucketsIndicesMemoryEnumerator GetEnumerator() => this;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<uint> IEnumerable<uint>.GetEnumerator() => GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MatchBits LoadMatch() => LoadMatch(ctrl.Span, index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static MatchBits LoadMatch(ReadOnlySpan<byte> ctrl, uint index)
        {
#if NET7_0_OR_GREATER
#if NET8_0_OR_GREATER
            if (Vector512.IsHardwareAccelerated)
            {
                return LoadMatch<Vector512<byte>>(ctrl, index);
            }
#endif
            if (Vector256.IsHardwareAccelerated)
            {
                return LoadMatch<Vector256<byte>>(ctrl, index);
            }
            if (Vector128.IsHardwareAccelerated)
            {
                return LoadMatch<Vector128<byte>>(ctrl, index);
            }
            if (Vector64.IsHardwareAccelerated)
            {
                return LoadMatch<Vector64<byte>>(ctrl, index);
            }
#endif
            {
                return LoadMatch<ulong>(ctrl, index);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static MatchBits LoadMatch<V>(ReadOnlySpan<byte> ctrl, uint index)
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
                    var pos = index + match.Offset;
                    if (pos >= slots_size) return false;
                    Current = pos;
                    match = match.Next;
                    return true;
                }
                if (index >= slots_size) return false;
                index += group_size;
                if (index >= slots_size) return false;
                match = LoadMatch();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            index = 0;
            match = LoadMatch(ctrl.Span, 0);
        }

        public uint Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private set;
        }

        object IEnumerator.Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Current;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() { }
    }

    #endregion
}

public abstract class ASwissTable<T, EH, H> : ASwissTable, IDisposable
    where EH : IEqHash<T> where H : IHasher
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
    private uint ctrl_size;
    /// <summary>
    /// The actual array size used, because the array is allocated from the pool, the array size may be larger than the used size
    /// <para><b>Must be power of two</b></para>
    /// </summary>
    private uint slots_size;
    /// <summary>
    /// How many elements are stored
    /// </summary>
    private int count;
    /// <summary>
    /// Mark when growth is needed
    /// </summary>
    private uint growth_count;
    /// <summary>
    /// Used to check if there are concurrent calls
    /// </summary>
    private int version;

    protected readonly H hasher;
    protected readonly EH eh;
    protected readonly ArrayPool<byte> poolCtrl;
    protected readonly ArrayPool<T> poolSlot;

    #endregion

    #region Getters

    /// <inheritdoc cref="count"/>
    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => count;
    }

    /// <inheritdoc cref="version"/>
    protected int Version
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => version;
    }

    /// <inheritdoc cref="ctrl"/>
    protected Span<byte> Ctrl
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ctrl_size == 0 ? default : ctrl.AsSpan(0, (int)(ctrl_size + CtrlGroupSize));
    }
    /// <inheritdoc cref="slots"/>
    protected Span<T> Slots
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => slots_size == 0 ? default : slots.AsSpan(0, (int)slots_size);
    }

    /// <inheritdoc cref="ctrl"/>
    protected Memory<byte> CtrlMemory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ctrl_size == 0 ? default : ctrl.AsMemory(0, (int)(ctrl_size + CtrlGroupSize));
    }
    /// <inheritdoc cref="slots"/>
    protected Memory<T> SlotsMemory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => slots_size == 0 ? default : slots.AsMemory(0, (int)slots_size);
    }

    protected byte[] BaseUnsafeGetCtrl() => ctrl;
    protected T[] BaseUnsafeGetSlot() => slots;

    #endregion

    #region Ctor

    protected ASwissTable(ArrayPoolFactory poolFactory, EH eh, H hasher, int cap)
    {
        if (cap < 0) throw new ArgumentOutOfRangeException(nameof(cap), "capacity must > 0");
        // ReSharper disable once VirtualMemberCallInConstructor
        HandleMustReturn(poolFactory.MustReturn);
        count = 0;
        this.eh = eh;
        this.hasher = hasher;
        poolCtrl = poolFactory.GetMayUninitialized<byte>();
        poolSlot = poolFactory.Get<T>();
        var groupSize = CtrlGroupSize;
        ctrl_size = cap == 0 ? 0 : ((uint)cap).CeilBinary(groupSize);
        slots_size = CeilCap((uint)cap);
        growth_count = MakeGrowth(slots_size);
        ctrl = poolCtrl.Rent((int)(ctrl_size == 0 ? 0 : ctrl_size + groupSize));
        slots = poolSlot.Rent((int)slots_size);
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

    #region Hash

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ulong Hash(ulong value) => hasher.Hash(value);

    #endregion

    #region Version

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void VersionPlus() => version = unchecked(version + 1);

    #endregion

    #region ModGetBucket

    /// <inheritdoc cref="ASwissTable.ModGetBucket(uint,uint)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected uint ModGetBucket(ulong v) => ModGetBucket(v, slots_size);

    #endregion

    #region ProbeSeq

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ProbeSeq H1StartProbe(ulong h1) => new(ModGetBucket(h1));

    #endregion

    #region TryFind

    protected bool TryFind<K, KEH>(in K key, in KEH keh, out uint slot_index) where KEH : IEqHashKey<T, K, EH>
    {
        Unsafe.SkipInit(out slot_index);
        if (slots_size == 0) return false;

        var hash = Hash((ulong)keh.CalcHash(in eh, in key));
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
    private bool TryFind<V, K, KEH>(in K key, in KEH keh, ulong h1, byte h2, out uint slot_index)
        where KEH : IEqHashKey<T, K, EH>
    {
        Unsafe.SkipInit(out slot_index);
        var size = slots_size;
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
        var hash = Hash((ulong)eh.CalcHash(in value));
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
    private bool TryInsert<V>(in T value, ulong h1, byte h2, InsertBehavior behavior)
    {
        if (slots_size == 0 || ShouldGrow) goto grow;
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
        VersionPlus();
        return true;
    }

    private enum SlotResult : byte
    {
        Fail,
        Got,
        NeedGrow,
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private SlotResult TryGetInsertSlot<V>(in T value, ulong h1, byte h2, InsertBehavior behavior,
        out uint insert_slot, out CtrlPos group_pos)
    {
        Unsafe.SkipInit(out insert_slot);
        Unsafe.SkipInit(out group_pos);
        var slots_size = this.slots_size;
        if (slots_size == 0) return SlotResult.NeedGrow;

        var ctrl_bytes = Ctrl;
        var slots = Slots;

        for (var prop = H1StartProbe(h1);; prop.MoveNext(slots_size))
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
    private bool TryGetInsertSlot<V>(ulong h1, out uint insert_slot, out CtrlPos ctrl_pos)
    {
        Unsafe.SkipInit(out insert_slot);
        Unsafe.SkipInit(out ctrl_pos);
        var slots_size = this.slots_size;
        if (slots_size == 0) return false;

        var ctrl_bytes = Ctrl;

        for (var prop = H1StartProbe(h1);; prop.MoveNext(slots_size))
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
        var size = this.slots_size;
        var ctrl_bytes = Ctrl;
        var pos = ctrl_pos.group + ctrl_pos.offset;
        var mod_pos = ModGetBucket(pos);
        ctrl_bytes[(int)mod_pos] = h2;
        if (mod_pos != pos)
        {
            ctrl_bytes[(int)pos] = h2;
        }
        if (size < CtrlGroupSize || mod_pos < CtrlGroupSize)
        {
            ctrl_bytes[(int)(size + mod_pos)] = h2;
        }
    }

    #endregion

    #region Grow

    protected bool ShouldGrow
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => count >= growth_count;
    }


    protected void Grow()
    {
        if (slots_size == 0) Grow(DefaultCapacity, true);
        // size is a power of 2
        else Grow(slots_size << 1, false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Grow(uint new_slots_size, bool init)
    {
        var old_slots_size = slots_size;
        var old_ctrl_size = ctrl_size;
        var old_ctrl = ctrl;
        var old_slots = slots;
        var old_growth_count = growth_count;

        var group_size = CtrlGroupSize;

        var new_ctrl_size = new_slots_size.CeilBinary(group_size);
        var new_ctrl_padded_size = new_ctrl_size + group_size;

        var new_growth_count = MakeGrowth(new_slots_size);

        var need_re_alloc_ctrl = init || new_ctrl_size != old_ctrl_size || new_ctrl_size != group_size;

        var new_ctrl = need_re_alloc_ctrl ? poolCtrl.Rent((int)new_ctrl_padded_size) : old_ctrl;
        var new_slots = poolSlot.Rent((int)new_slots_size);

        if (need_re_alloc_ctrl)
            new_ctrl.AsSpan().Fill(SlotIsEmpty);

        slots_size = new_slots_size;
        ctrl_size = new_ctrl_size;
        ctrl = new_ctrl;
        slots = new_slots;
        growth_count = new_growth_count;
        VersionPlus();

        if (!init)
        {
            try
            {
                if (need_re_alloc_ctrl)
                {
                    ReInsert(old_ctrl.AsSpan(0, (int)old_ctrl_size), old_slots, old_slots_size, group_size);
                }
                else
                {
                    var new_ctrl_tmp = old_ctrl.AsSpan();
                    Span<byte> old_ctrl_tmp = stackalloc byte[new_ctrl_tmp.Length];
                    new_ctrl_tmp.CopyTo(old_ctrl_tmp);
                    new_ctrl_tmp.Fill(SlotIsEmpty);

                    try
                    {
                        ReInsert(old_ctrl_tmp[..(int)old_ctrl_size], old_slots, old_slots_size, group_size);
                    }
                    catch
                    {
                        old_ctrl_tmp.CopyTo(new_ctrl_tmp);
                        throw;
                    }
                }
            }
            catch
            {
                slots_size = old_slots_size;
                ctrl_size = old_ctrl_size;
                ctrl = old_ctrl;
                slots = old_slots;
                growth_count = old_growth_count;

                if (need_re_alloc_ctrl)
                    poolCtrl.Return(new_ctrl);
                poolSlot.Return(new_slots, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                throw;
            }
        }

        if (need_re_alloc_ctrl)
            poolCtrl.Return(old_ctrl);
        poolSlot.Return(old_slots, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReInsert(ReadOnlySpan<byte> old_ctrl, ReadOnlySpan<T> old_slots, uint old_slots_size, uint group_size)
    {
#if NET7_0_OR_GREATER
#if NET8_0_OR_GREATER
        if (Vector512.IsHardwareAccelerated)
        {
            ReInsert<Vector512<byte>>(old_ctrl, old_slots, old_slots_size, group_size);
            return;
        }
#endif
        if (Vector256.IsHardwareAccelerated)
        {
            ReInsert<Vector256<byte>>(old_ctrl, old_slots, old_slots_size, group_size);
            return;
        }
        if (Vector128.IsHardwareAccelerated)
        {
            ReInsert<Vector128<byte>>(old_ctrl, old_slots, old_slots_size, group_size);
            return;
        }
        if (Vector64.IsHardwareAccelerated)
        {
            ReInsert<Vector64<byte>>(old_ctrl, old_slots, old_slots_size, group_size);
            return;
        }
#endif
        {
            ReInsert<ulong>(old_ctrl, old_slots, old_slots_size, group_size);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReInsert<V>(ReadOnlySpan<byte> old_ctrl, ReadOnlySpan<T> old_slots, uint old_slots_size,
        uint group_size)
    {
        var slots = Slots;
        foreach (var old_index in new FullBucketsIndicesSpanEnumerator<V>(
                     old_ctrl, old_slots_size, group_size))
        {
            ref readonly var old_slot = ref old_slots[(int)old_index];

            var hash = Hash((ulong)eh.CalcHash(in old_slot));
            var h1 = GetH1(hash);
            var h2 = GetH2(hash);

            // ReSharper disable once RedundantAssignment
            var success = TryGetInsertSlot<V>(h1, out var insert_slot, out var ctrl_pos);
            Debug.Assert(success);

            slots[(int)insert_slot] = old_slot;
            WriteCtrl(ctrl_pos, h2);
            VersionPlus();
        }
    }

    #endregion

    #region Remove

    protected bool TryRemove<K, KEH>(in K key, in KEH keh, out T value) where KEH : IEqHashKey<T, K, EH>
    {
        Unsafe.SkipInit(out value);
        if (slots_size == 0 || count == 0) return false;
        
        var hash = Hash((ulong)keh.CalcHash(in eh, in key));
        var h1 = GetH1(hash);
        var h2 = GetH2(hash);
        
#if NET7_0_OR_GREATER
#if NET8_0_OR_GREATER
        if (Vector512.IsHardwareAccelerated)
            return TryRemove<Vector512<byte>, K, KEH>(in key, in keh, h1, h2, out value);
#endif
        if (Vector256.IsHardwareAccelerated)
            return TryRemove<Vector256<byte>, K, KEH>(in key, in keh, h1, h2, out value);
        if (Vector128.IsHardwareAccelerated)
            return TryRemove<Vector128<byte>, K, KEH>(in key, in keh, h1, h2, out value);
        if (Vector64.IsHardwareAccelerated)
            return TryRemove<Vector64<byte>, K, KEH>(in key, in keh, h1, h2, out value);
#endif
        return TryRemove<ulong, K, KEH>(in key, in keh, h1, h2, out value);
    }
    
    private bool TryRemove<V, K, KEH>(in K key, in KEH keh, ulong h1, byte h2, out T value) where KEH : IEqHashKey<T, K, EH>
    {
        Unsafe.SkipInit(out value);
        var size = slots_size;
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
                    WriteCtrl(new(prop.pos, offset), SlotIsDeleted); // todo check empty
                    value = slot;
                    if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                        slot = default!;
                    return true;
                }
            }
            if (MatchEmpty(ref group)) return false;
        }
    }

    #endregion

    #region Clear

    public void Clear()
    {
        count = 0;
        Ctrl.Fill(SlotIsEmpty);
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) Slots.Clear();
    }

    #endregion

    #region Enumerator

    protected FullBucketsIndicesMemoryEnumerator MakeIndicesEnumerator()
        => new(CtrlMemory, slots_size, CtrlGroupSize);

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
