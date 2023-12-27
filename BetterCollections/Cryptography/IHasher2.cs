using System;
using System.Collections.Generic;

namespace BetterCollections.Cryptography;

public interface IHasher2
{
    public void Add<T>(T value);
    public void Add<T>(T value, IEqualityComparer<T>? comparer);
    public void AddBytes(ReadOnlySpan<byte> value);
    public void AddString(ReadOnlySpan<byte> value);
    public void AddString(ReadOnlySpan<char> value);
    public int ToHashCode();
    public long ToHashCodeLong();
}
