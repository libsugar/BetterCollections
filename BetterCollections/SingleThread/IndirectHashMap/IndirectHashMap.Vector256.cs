#if NET7_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using BetterCollections.IndirectHashMap_Internal;
using BetterCollections.Misc;

namespace BetterCollections;

public partial class IndirectHashMap<TKey, TValue>
{
    private static partial class Vector256Impl
    {
        public const int GroupSize = 32;

        #region Init

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InitTable(IndirectHashMap<TKey, TValue> self, int cap, ref Table table, bool clear)
        {
            if (cap > 0) cap = cap.CeilPowerOf2();
            if (cap != 0 && cap < DefaultCapacity) cap = DefaultCapacity;
            var ctrlSize = cap == 0 ? 0 : cap.CeilBinary(GroupSize) / GroupSize;
            var slotSize = cap;
            table.ctrlSizeMinusOne = ctrlSize == 0 ? 0 : ctrlSize - 1;
            table.slotSizeMinusOne = slotSize == 0 ? 0 : slotSize - 1;
            table.poolCtrl.Vector256 = self.poolFactory.GetMayUninitialized<Vector256<byte>>();
            table.ctrlArray.Vector256 = ctrlSize == 0 ? null : table.poolCtrl.Vector256.Rent(ctrlSize);
            table.metaArray = cap == 0 ? null : self.poolMetas.Rent(cap);
            table.entryArray = cap == 0 ? null : self.poolEntries.Rent(cap);
            table.entryIndex = 0;
            table.growthCount = MakeGrowth(slotSize);
            table.groupType = GroupType.Vector256;
            if (clear && cap > 0)
            {
                var ctrlArray = table.ctrlArray.Vector256;
                ctrlArray.AsSpan(0, ctrlSize).Fill(Vector256<byte>.AllBitsSet);

                var metaArray = table.metaArray;
                metaArray.AsSpan(0, slotSize).Fill(new(0, -1));
            }
        }

        #endregion

        #region First Init

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FirstInit(IndirectHashMap<TKey, TValue> self, ref Table table)
        {
            var new_ctrlSize = 1;
            var new_slotSize = DefaultCapacity;

            var new_ctrlArray = table.poolCtrl.Vector256.Rent(new_ctrlSize);
            var new_metaArray = self.poolMetas.Rent(new_slotSize);
            var new_entryArray = self.poolEntries.Rent(new_slotSize);

            new_ctrlArray[0] = Vector256.Create(SlotIsEmpty);

            table.ctrlSizeMinusOne = new_ctrlSize - 1;
            table.slotSizeMinusOne = new_slotSize - 1;

            table.ctrlArray.Vector256 = new_ctrlArray;
            table.metaArray = new_metaArray;
            table.entryArray = new_entryArray;
        }

        #endregion

        #region TryFind

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue TryFind(IndirectHashMap<TKey, TValue> self, TKey key)
        {
            ref var table = ref self.table;

            if (table.entryArray == null) return ref Unsafe.NullRef<TValue>();

            self.CalcHash(ref key, out var h1, out var h2);

            #region Try Find Item

            {
                var ctrl_array = table.ctrlArray.Vector256!;
                var meta_array = table.metaArray!;
                var entry_array = table.entryArray!;

                var group_count_m1 = table.ctrlSizeMinusOne;

                #region Small

                if (group_count_m1 == 0)
                {
                    ref var group = ref ctrl_array[0];
                    var cmp = Vector256.Equals(group, Vector256.Create(h2));
                    var match = cmp.ExtractMostSignificantBits();

                    for (; match != 0; match &= match - 1)
                    {
                        var pos = (int)uint.TrailingZeroCount(match);
                        ref var entry = ref entry_array[pos];

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
                    }

                    return ref Unsafe.NullRef<TValue>();
                }

                #endregion

                var slot_count_m1 = table.slotSizeMinusOne;

                for (;;)
                {
                    throw new NotImplementedException("todo");
                }
            }

            #endregion
        }

        #endregion

        #region TryInsert

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryInsert(IndirectHashMap<TKey, TValue> self,
            TKey key, TValue value, InsertBehavior behavior)
        {
            self.CalcHash(ref key, out var h1, out var h2);

            ref var table = ref self.table;

            if (table.entryArray == null)
            {
                FirstInit(self, ref table);
                Debug.Assert(self.table.entryArray != null);
                Debug.Assert(self.table.metaArray != null);
                Debug.Assert(self.table.ctrlArray.Vector256 != null);
                goto do_insert_no_grow;
            }

            #region Try Find Item

            {
                var ctrl_array = table.ctrlArray.Vector256!;
                var meta_array = table.metaArray!;
                var entry_array = table.entryArray!;

                var group_count_m1 = table.ctrlSizeMinusOne;

                #region Small

                if (group_count_m1 == 0)
                {
                    ref var group = ref ctrl_array[0];
                    var cmp = Vector256.Equals(group, Vector256.Create(h2));
                    var match = cmp.ExtractMostSignificantBits();

                    for (; match != 0; match &= match - 1)
                    {
                        var pos = (int)uint.TrailingZeroCount(match);
                        ref var entry = ref entry_array[pos];

                        if (typeof(TKey).IsValueType && self.comparer == null)
                        {
                            if (EqualityComparer<TKey>.Default.Equals(key, entry.Key))
                            {
                                if (behavior != InsertBehavior.OverwriteIfExisting) return false;
                                entry.Value = value;
                                return true;
                            }
                        }
                        else
                        {
                            if (self.comparer!.Equals(key, entry.Key))
                            {
                                if (behavior != InsertBehavior.OverwriteIfExisting) return false;
                                entry.Value = value;
                                return true;
                            }
                        }
                    }

                    goto do_insert;
                }

                #endregion

                var slot_count_m1 = table.slotSizeMinusOne;

                for (;;)
                {
                    throw new NotImplementedException("todo");
                }
            }

            #endregion

            do_insert:

            if (ShouldGrow(self.count, table.growthCount))
            {
                ReSize(self);
            }

            do_insert_no_grow:

            #region Try Find Empty or Delete

            {
                var ctrl_array = table.ctrlArray.Vector256!;

                var group_count_m1 = table.ctrlSizeMinusOne;
                var slot_count_m1 = table.slotSizeMinusOne;

                int insert_pos;

                if (group_count_m1 == 0)
                {
                    ref var group = ref ctrl_array[0];
                    insert_pos = (int)uint.TrailingZeroCount(group.ExtractMostSignificantBits());
                    Unsafe.Add(ref Unsafe.As<Vector256<byte>, byte>(ref group), insert_pos) = h2;
                    goto insert_small;
                }

                for (;;)
                {
                    var group_pos = h1 & group_count_m1;
                    var next_group_pos = group_pos & group_count_m1;
                    var in_group_offset = h1 & slot_count_m1 & (GroupSize - 1);

                    throw new NotImplementedException();
                }

                // insert_new:
                // {
                //     var meta_array = table.metaArray!;
                //     var entry_array = table.entryArray!;
                //
                //     var entry_index = table.entryIndex++;
                //
                //     ref var meta = ref meta_array[insert_pos];
                //     meta.HashCode = h1;
                //     meta.Index = entry_index;
                //
                //     ref var entry = ref entry_array[entry_index];
                //     entry.Key = key!;
                //     entry.Value = value;
                //
                //     return true;
                // }
                //
                // insert_del:
                // {
                //     var meta_array = table.metaArray!;
                //     var entry_array = table.entryArray!;
                //
                //     ref var meta = ref meta_array[insert_pos];
                //     meta.HashCode = h1;
                //
                //     ref var entry = ref entry_array[meta.Index];
                //     entry.Key = key!;
                //     entry.Value = value;
                //
                //     return true;
                // }

                insert_small:
                {
                    var meta_array = table.metaArray!;
                    var entry_array = table.entryArray!;

                    ref var meta = ref meta_array[insert_pos];
                    meta.HashCode = h1;
                    meta.Index = insert_pos;

                    ref var entry = ref entry_array[insert_pos];
                    entry.Key = key!;
                    entry.Value = value;

                    self.count++;
                    self.version++;

                    return true;
                }
            }

            #endregion
        }

        #endregion

        #region ReSize

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReSize(IndirectHashMap<TKey, TValue> self)
        {
            ref var old_table = ref self.table;
            Table new_table = default;
            new_table.poolCtrl = old_table.poolCtrl;
            new_table.groupType = old_table.groupType;

            var old_slot_size = old_table.slotSizeMinusOne + 1;
            var new_slot_size = old_slot_size << 1;

            var new_ctrl_size = new_slot_size.CeilBinary(GroupSize) / GroupSize;

            if (new_ctrl_size == 1)
            {
                new_table.metaArray = self.poolMetas.Rent(new_slot_size);
                new_table.entryArray = self.poolEntries.Rent(new_slot_size);

                var entry_index = old_table.entryIndex;

                old_table.metaArray.AsSpan(entry_index).CopyTo(new_table.metaArray.AsSpan(entry_index));
                old_table.entryArray.AsSpan(entry_index).CopyTo(new_table.entryArray.AsSpan(entry_index));

                new_table.ctrlArray = old_table.ctrlArray;
                new_table.ctrlSizeMinusOne = old_table.ctrlSizeMinusOne;
                new_table.slotSizeMinusOne = new_slot_size - 1;
                new_table.entryIndex = entry_index;
                new_table.growthCount = MakeGrowth(new_slot_size);

                var old_metaArray = old_table.metaArray;
                var old_entryArray = old_table.entryArray;

                old_table = new_table;
                self.poolMetas.Return(old_metaArray!);
                self.poolEntries.Return(old_entryArray!, RuntimeHelpers.IsReferenceOrContainsReferences<Entry>());
            }
            else
            {
                var ctrlArray = new_table.ctrlArray.Vector256 = new_table.poolCtrl.Vector256.Rent(new_ctrl_size);
                new_table.metaArray = self.poolMetas.Rent(new_slot_size);
                new_table.entryArray = self.poolEntries.Rent(new_slot_size);

                var entry_index = old_table.entryIndex;

                ctrlArray.AsSpan().Fill(Vector256<byte>.AllBitsSet);
                new_table.metaArray.AsSpan(0, new_slot_size).Fill(new(0, -1));
                old_table.entryArray.AsSpan(entry_index).CopyTo(new_table.entryArray.AsSpan(entry_index));

                throw new NotImplementedException("todo");
            }
        }

        #endregion
    }
}

#endif
