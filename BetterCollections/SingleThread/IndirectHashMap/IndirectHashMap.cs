using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using BetterCollections.Exceptions;
using BetterCollections.Misc;

namespace BetterCollections;

public partial class IndirectHashMap<TKey, TValue> : IDictionary<TKey, TValue>
{
    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => count;
    }
    public bool IsReadOnly
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => false;
    }

    public TValue this[TKey key]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ref var value = ref TryGetValue(key);
            if (Unsafe.IsNullRef(ref value)) throw new KeyNotFoundException();
            return value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
#if NET7_0_OR_GREATER
            // ReSharper disable once RedundantAssignment
            var r = table.groupType switch
            {
                GroupType.Vector64 => throw new NotImplementedException(),
                GroupType.Vector128 => throw new NotImplementedException(),
                GroupType.Vector256 => Vector256Impl.TryInsert(this, key, value, InsertBehavior.FailureIfExisting),
#if NET8_0_OR_GREATER
                GroupType.Vector512 => throw new NotImplementedException(),
#endif
                _ => throw new NotImplementedException(), // ulong
            };
#else
            bool r = true; // todo
#endif
            Debug.Assert(r);
        }
    }

    #region TryGetValue

#if NET6_0_OR_GREATER
    [SkipLocalsInit]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(TKey key, out TValue value)
    {
        ref var value_ref = ref TryGetValue(key);
        if (Unsafe.IsNullRef(ref value_ref))
        {
            value = default!;
            return false;
        }
        else
        {
            value = value_ref;
            return true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref TValue TryGetValue(TKey key)
    {
        ref TValue value = ref Unsafe.NullRef<TValue>();
#if NET7_0_OR_GREATER
        switch (table.groupType)
        {
            case GroupType.Vector64:
                throw new NotImplementedException();
            case GroupType.Vector128:
                throw new NotImplementedException();
            case GroupType.Vector256:
                value = ref Vector256Impl.TryFind(this, key);
                break;
#if NET8_0_OR_GREATER
            case GroupType.Vector512:
                throw new NotImplementedException();
#endif
            default:
                throw new NotImplementedException();
        }
#else
#endif
        return ref value;
    }

    #endregion

    #region ContainsKey

#if NET6_0_OR_GREATER
    [SkipLocalsInit]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(TKey key)
    {
        ref var value = ref TryGetValue(key);
        if (Unsafe.IsNullRef(ref value)) return false;
        return true;
    }

    #endregion

    #region Add

#if NET6_0_OR_GREATER
    [SkipLocalsInit]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(TKey key, TValue value)
    {
#if NET7_0_OR_GREATER
        var r = table.groupType switch
        {
            GroupType.Vector64 => throw new NotImplementedException(),
            GroupType.Vector128 => throw new NotImplementedException(),
            GroupType.Vector256 => Vector256Impl.TryInsert(this, key, value, InsertBehavior.FailureIfExisting),
#if NET8_0_OR_GREATER
            GroupType.Vector512 => throw new NotImplementedException(),
#endif
            _ => throw new NotImplementedException(), // ulong
        };
#else
        bool r = true; // todo
#endif
        if (!r) throw new DuplicateKeyException();
    }

    #endregion

    #region Remove

#if NET6_0_OR_GREATER
    [SkipLocalsInit]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(TKey key)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region Clear

#if NET6_0_OR_GREATER
    [SkipLocalsInit]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        throw new NotImplementedException();
    }

    #endregion

    #region Add KeyValuePair

#if NET6_0_OR_GREATER
    [SkipLocalsInit]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    #endregion

    #region Contains KeyValuePair

#if NET6_0_OR_GREATER
    [SkipLocalsInit]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);

    #endregion

    #region Remove KeyValuePair

#if NET6_0_OR_GREATER
    [SkipLocalsInit]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);

    #endregion

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public ICollection<TKey> Keys
    {
        get => throw new NotImplementedException();
    }
    public ICollection<TValue> Values
    {
        get => throw new NotImplementedException();
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
