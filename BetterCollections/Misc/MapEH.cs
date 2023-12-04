using System.Collections.Generic;

namespace BetterCollections.Misc;

public readonly struct MapEH<TKey, TValue>(IEqualityComparer<TKey>? comparer) : IEqHash<(TKey Key, TValue Value)>
{
    private readonly IEqualityComparer<TKey>? comparer =
        comparer ?? (typeof(TKey).IsValueType ? null : EqualityComparer<TKey>.Default);

    public bool IsEq(in (TKey Key, TValue Value) a, in (TKey Key, TValue Value) b)
    {
        if (typeof(TKey).IsValueType && comparer == null)
            return EqualityComparer<TKey>.Default.Equals(a.Key, b.Key);
        return comparer!.Equals(a.Key, b.Key);
    }

    public int CalcHash(in (TKey Key, TValue Value) value)
    {
        if (typeof(TKey).IsValueType && comparer == null)
            return EqualityComparer<TKey>.Default.GetHashCode(value.Key!);
        if (value.Key == null) return 0;
        return comparer!.GetHashCode(value.Key);
    }
}
