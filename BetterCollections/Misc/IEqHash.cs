using System.Runtime.CompilerServices;

namespace BetterCollections.Misc;

public interface IEqHash<T>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEq(in T a, in T b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CalcHash(in T value);
}
