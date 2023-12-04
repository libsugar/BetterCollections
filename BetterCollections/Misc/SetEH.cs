using System.Collections.Generic;

namespace BetterCollections.Misc;

public readonly struct SetEH<T>(IEqualityComparer<T>? comparer) : IEqHash<T>
{
    private readonly IEqualityComparer<T>? comparer =
        comparer ?? (typeof(T).IsValueType ? null : EqualityComparer<T>.Default);

    public bool IsEq(in T a, in T b)
    {
        if (typeof(T).IsValueType && comparer == null)
            return EqualityComparer<T>.Default.Equals(a, b);
        return comparer!.Equals(a, b);
    }

    public int CalcHash(in T value)
    {
        if (typeof(T).IsValueType && comparer == null)
            return EqualityComparer<T>.Default.GetHashCode(value!);
        if (value == null) return 0;
        return comparer!.GetHashCode(value);
    }
}
