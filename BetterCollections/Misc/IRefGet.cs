using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BetterCollections.Misc;

public interface IGetter<out T, S>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get(in S source);
}

public readonly struct IdentityGetter<T> : IGetter<T, T>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get(in T source) => source;
}

public readonly struct KeyValueGetter<K, V> : IGetter<KeyValuePair<K, V>, (K Key, V Value)>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public KeyValuePair<K, V> Get(in (K Key, V Value) source)
        => new(source.Key, source.Value);
}

public readonly struct KeyGetter<K, V> : IGetter<K, (K Key, V Value)>, IGetter<K, KeyValuePair<K, V>>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public K Get(in (K Key, V Value) source)
        => source.Key;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public K Get(in KeyValuePair<K, V> source)
        => source.Key;
}

public readonly struct ValueGetter<K, V> : IGetter<V, (K Key, V Value)>, IGetter<V, KeyValuePair<K, V>>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public V Get(in (K Key, V Value) source)
        => source.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public V Get(in KeyValuePair<K, V> source)
        => source.Value;
}
