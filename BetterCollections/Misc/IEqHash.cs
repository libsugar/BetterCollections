using System.Runtime.CompilerServices;

namespace BetterCollections.Misc;

public interface IEqHash<T>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEq(in T a, in T b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CalcHash(in T value);
}

public interface IEqHashKey<T, K, S> where S : IEqHash<T>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEq(in S self, in K a, in T b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CalcHash(in S self, in K value);
}
