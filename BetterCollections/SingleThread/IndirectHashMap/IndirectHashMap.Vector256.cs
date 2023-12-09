#if NET7_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using BetterCollections.Misc;

namespace BetterCollections;

public partial class IndirectHashMap<TKey, TValue>
{
    private static partial class Vector256Impl
    {
        public const int GroupSize = 32;

        #region Init

        [SkipLocalsInit]
        public static void InitTable(IndirectHashMap<TKey, TValue> self, int cap, ref Table table, bool clear)
        {
            if (cap > 0) cap = cap.CeilPowerOf2();
            if (cap != 0 && cap < DefaultCapacity) cap = DefaultCapacity;
            var slotSize = cap;
            var ctrlSize = cap == 0 ? 0 : cap + GroupSize;
            table.slotSizeMinusOne = slotSize == 0 ? 0 : slotSize - 1;
            table.ctrlArray = ctrlSize == 0 ? null : self.poolBytes.Rent(ctrlSize.CeilBinary(GroupSize));
            table.entryArray = cap == 0 ? null : self.poolEntries.Rent(cap);
            table.growthCount = MakeGrowth(slotSize);
            table.groupType = GroupType.Vector256;
            if (clear && cap > 0)
            {
                table.ctrlArray.AsSpan(0, ctrlSize).Fill(SlotIsEmpty);
            }
        }

        #endregion

        #region First Init

        [SkipLocalsInit]
        public static void FirstInit(IndirectHashMap<TKey, TValue> self, ref Table table)
        {
            var new_slotSize = DefaultCapacity;

            var new_ctrlSize = new_slotSize + GroupSize;

            var new_ctrlArray = self.poolBytes.Rent(new_ctrlSize.CeilBinary(GroupSize));
            var new_entryArray = self.poolEntries.Rent(new_slotSize);

            new_ctrlArray.AsSpan(0, new_ctrlSize).Fill(SlotIsEmpty);

            table.slotSizeMinusOne = new_slotSize - 1;

            table.ctrlArray = new_ctrlArray;
            table.entryArray = new_entryArray;

            table.growthCount = MakeGrowth(new_slotSize);

            self.version++;
        }

        #endregion

        #region TryFind

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue TryFind(IndirectHashMap<TKey, TValue> self, TKey key)
        {
            ref readonly var table = ref self.table;

            if (table.entryArray == null) return ref Unsafe.NullRef<TValue>();

            self.CalcHash(key, out var h1, out var h2);

            ref readonly var ctrl_array = ref table.ctrlArray!;
            ref readonly var entry_array = ref table.entryArray!;

            ref readonly var slot_size_m1 = ref table.slotSizeMinusOne;

            for (uint pos = h1 & (uint)slot_size_m1, stride = 0u;;
                 stride += GroupSize, pos += stride, pos &= (uint)slot_size_m1)
            {
                Debug.Assert(stride <= slot_size_m1);

                var group = Vector256.LoadUnsafe(ref ctrl_array[pos]);
                var cmp = Vector256.Equals(group, Vector256.Create(h2));
                var match = cmp.ExtractMostSignificantBits();

                for (; match != 0; match &= match - 1)
                {
                    var offset = (int)uint.TrailingZeroCount(match);
                    var index = (pos + offset) & slot_size_m1;
                    ref var entry = ref entry_array[index];

                    #region Eq

                    if (typeof(TKey).IsValueType && self.comparer == null)
                    {
                        if (EqualityComparer<TKey>.Default.Equals(key, entry.Key))
                        {
                            return ref entry.Value;
                        }
                    }
                    else
                    {
                        if (self.comparer!.Equals(key, entry.Key))
                        {
                            return ref entry.Value;
                        }
                    }

                    #endregion
                }

                if (group.ExtractMostSignificantBits() != 0) return ref Unsafe.NullRef<TValue>();
            }
        }

        #endregion

        #region TryInsert

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryInsert(IndirectHashMap<TKey, TValue> self,
            TKey key, TValue value, InsertBehavior behavior)
        {
            ref var table = ref self.table;

            if (table.entryArray == null!)
            {
                FirstInit(self, ref table);
                Debug.Assert(table.entryArray != null);
                Debug.Assert(table.ctrlArray != null);
            }
            else if (self.count >= table.growthCount)
            {
                ReSize(self);
            }

            return TryInsertNoGrow(self, key, value, behavior);
        }

        #endregion

        #region TryInsertNoGrow

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryInsertNoGrow(IndirectHashMap<TKey, TValue> self,
            TKey key, TValue value, InsertBehavior behavior)
        {
            ref var table = ref self.table;

            self.CalcHash(key, out var h1, out var h2);

            ref readonly var ctrl_array = ref table.ctrlArray!;
            ref readonly var entry_array = ref table.entryArray!;

            var slot_size_m1 = table.slotSizeMinusOne;

            uint insert_pos = uint.MaxValue;

            for (uint pos = h1 & (uint)slot_size_m1, stride = 0u;;
                 stride += GroupSize, pos += stride, pos &= (uint)slot_size_m1)
            {
                Debug.Assert(stride <= slot_size_m1);

                var group = Vector256.LoadUnsafe(ref ctrl_array[pos]);

                #region Try Find Item

                {
                    var cmp = Vector256.Equals(group, Vector256.Create(h2));
                    var match = cmp.ExtractMostSignificantBits();

                    for (; match != 0; match &= match - 1)
                    {
                        var offset = (int)uint.TrailingZeroCount(match);
                        var index = (pos + offset) & slot_size_m1;
                        ref var entry = ref entry_array[index];

                        #region Eq

                        if (typeof(TKey).IsValueType && self.comparer == null)
                        {
                            if (EqualityComparer<TKey>.Default.Equals(key, entry.Key))
                            {
                                if (behavior != InsertBehavior.OverwriteIfExisting) return false;
                                entry.Value = value;
                                self.version++;
                                return true;
                            }
                        }
                        else
                        {
                            if (self.comparer!.Equals(key, entry.Key))
                            {
                                if (behavior != InsertBehavior.OverwriteIfExisting) return false;
                                entry.Value = value;
                                self.version++;
                                return true;
                            }
                        }

                        #endregion
                    }
                }

                #endregion

                #region Try Find Empty Or Delete

                if (insert_pos == uint.MaxValue)
                {
                    var match = group.ExtractMostSignificantBits();
                    insert_pos = match != 0
                        ? (pos + uint.TrailingZeroCount(match)) & (uint)slot_size_m1
                        : uint.MaxValue;
                }

                if (group.ExtractMostSignificantBits() != 0)
                {
                    Debug.Assert(insert_pos != uint.MaxValue);
                    if ((ctrl_array[insert_pos] & SlotIsDeleted) == 0)
                    {
                        insert_pos = uint.TrailingZeroCount(Vector256.LoadUnsafe(ref ctrl_array[0])
                            .ExtractMostSignificantBits());
                    }
                    entry_array[insert_pos] = new(key, value, h1);
                    ctrl_array[insert_pos] = h2;
                    self.count++;
                    self.version++;
                    return true;
                }

                #endregion
            }
        }

        #endregion

        #region ReSize

        [SkipLocalsInit]
        public static void ReSize(IndirectHashMap<TKey, TValue> self)
        {
            ref var table = ref self.table;

            var old_ctrlArray = table.ctrlArray!;
            var old_entryArray = table.entryArray!;
            var old_slotSizeMinusOne = table.slotSizeMinusOne;

            var new_slotSize = (table.slotSizeMinusOne + 1) << 1;
            var new_ctrlSize = new_slotSize + GroupSize;

            var new_ctrlArray = self.poolBytes.Rent(new_ctrlSize.CeilBinary(GroupSize));
            var new_entryArray = self.poolEntries.Rent(new_slotSize);

            try
            {
                new_ctrlArray.AsSpan(0, new_ctrlSize).Fill(SlotIsEmpty);

                table.slotSizeMinusOne = new_slotSize - 1;

                table.ctrlArray = new_ctrlArray;
                table.entryArray = new_entryArray;

                self.version++;

                ReInsert(self, ref old_ctrlArray, ref old_entryArray);
            }
            catch
            {
                table.slotSizeMinusOne = old_slotSizeMinusOne;

                table.ctrlArray = old_ctrlArray;
                table.entryArray = old_entryArray;

                self.poolBytes.Return(new_ctrlArray);
                self.poolEntries.Return(new_entryArray, RuntimeHelpers.IsReferenceOrContainsReferences<Entry>());

                self.version++;

                throw;
            }

            table.growthCount = MakeGrowth(new_slotSize);

            self.poolBytes.Return(old_ctrlArray);
            self.poolEntries.Return(old_entryArray, RuntimeHelpers.IsReferenceOrContainsReferences<Entry>());

            self.version++;
        }

        #endregion

        #region ReInsert

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReInsert(
            IndirectHashMap<TKey, TValue> self,
            ref readonly byte[] old_ctrlArray,
            ref readonly Entry[] old_entryArray
        )
        {
            ref var table = ref self.table;

            ref readonly var ctrl_array = ref table.ctrlArray!;
            ref readonly var entry_array = ref table.entryArray!;

            var slot_size_m1 = table.slotSizeMinusOne;

            var count = self.count;
            for (var group_pos = 0; group_pos < old_ctrlArray.Length; group_pos += GroupSize)
            {
                var old_group = Vector256.LoadUnsafe(ref old_ctrlArray[group_pos]);
                var old_match = (~old_group).ExtractMostSignificantBits();

                for (; old_match != 0; old_match &= old_match - 1)
                {
                    var old_pos = group_pos + (int)uint.TrailingZeroCount(old_match);
                    if (old_pos > count) return;

                    ref var old_entry = ref old_entryArray[old_pos];
                    var h1 = old_entry.HashCode;
                    var h2 = old_ctrlArray[old_pos];

                    for (uint pos = h1 & (uint)slot_size_m1, stride = 0u;;
                         stride += GroupSize, pos += stride, pos &= (uint)slot_size_m1)
                    {
                        Debug.Assert(stride <= slot_size_m1);

                        var group = Vector256.LoadUnsafe(ref ctrl_array[pos]);
                        var match = group.ExtractMostSignificantBits();

                        if (match != 0)
                        {
                            var insert_pos = (pos + uint.TrailingZeroCount(match)) & (uint)slot_size_m1;
                            if ((ctrl_array[insert_pos] & SlotIsDeleted) == 0)
                            {
                                insert_pos = uint.TrailingZeroCount(Vector256.LoadUnsafe(ref ctrl_array[0])
                                    .ExtractMostSignificantBits());
                            }
                            entry_array[insert_pos] = old_entry;
                            ctrl_array[insert_pos] = h2;
                            self.count++;
                            self.version++;
                            break;
                        }
                    }
                }
            }
        }

        #endregion
    }
}

#endif
