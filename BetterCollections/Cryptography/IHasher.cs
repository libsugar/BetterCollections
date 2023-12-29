using System;
using System.Collections.Generic;

namespace BetterCollections.Cryptography;

public interface IHasher
{
    /// <inheritdoc cref="HashCode.Add{T}(T)"/>
    public void Add<T>(T value);

    /// <inheritdoc cref="HashCode.Add{T}(T, IEqualityComparer{T}?)"/>
    public void Add<T>(T value, IEqualityComparer<T>? comparer);

    /// <inheritdoc cref="HashCode.AddBytes(ReadOnlySpan{byte})"/>
    public void AddBytes(ReadOnlySpan<byte> value);

    /// <inheritdoc cref="AddString(ReadOnlySpan{char})"/>
    public void AddString(ReadOnlySpan<byte> value);

    /// <summary>
    /// Adds a span of chars to the hash code.
    /// </summary>
    /// <param name="value">The span to add.</param>
    public void AddString(ReadOnlySpan<char> value);

    /// <summary>
    /// Calculates the final hash code after consecutive AHasher.Add invocations.
    /// </summary>
    /// <returns>The calculated hash code.</returns>
    public int ToHashCode();

    /// <inheritdoc cref="ToHashCode()"/>
    public long ToHashCodeLong();
}
