using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BetterCollections.Misc;

public readonly struct EqHash<TKey, TValue>(IEqualityComparer<TKey>? comparer) :
    IEqHash<(TKey Key, TValue Value)>,
    IEqHash<TKey>
{
    private readonly IEqualityComparer<TKey>? comparer =
        comparer ?? (typeof(TKey).IsValueType ? null : EqualityComparer<TKey>.Default);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEq(in TKey a, in TKey b)
    {
        if (typeof(TKey).IsValueType && comparer == null)
            return EqualityComparer<TKey>.Default.Equals(a, b);
        return comparer!.Equals(a, b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CalcHash(in TKey value)
    {
        if (typeof(TKey).IsValueType && comparer == null)
            return EqualityComparer<TKey>.Default.GetHashCode(value!);
        if (value == null) return 0;
        return comparer!.GetHashCode(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEq(in (TKey Key, TValue Value) a, in (TKey Key, TValue Value) b)
        => IsEq(a.Key, b.Key);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CalcHash(in (TKey Key, TValue Value) value)
        => CalcHash(value.Key);
}

public readonly struct EqHashKey<TKey, TValue> :
    IEqHashKey<(TKey Key, TValue Value), TKey, EqHash<TKey, TValue>>,
    IEqHashKey<TKey, TKey, EqHash<TKey, TValue>>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEq(in EqHash<TKey, TValue> self, in TKey a, in (TKey Key, TValue Value) b)
        => self.IsEq(a, b.Key);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEq(in EqHash<TKey, TValue> self, in TKey a, in TKey b)
        => self.IsEq(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CalcHash(in EqHash<TKey, TValue> self, in TKey value)
        => self.CalcHash(value);
}
