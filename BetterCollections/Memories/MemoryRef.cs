using System;
using System.Runtime.CompilerServices;

namespace BetterCollections.Memories;

public readonly struct MemoryRef<T>(Memory<T> memory, int offset) :
    IEquatable<MemoryRef<T>>, IRef<T>, IReadOnlyRef<T>
{
    private readonly Memory<T> memory = memory;
    private readonly int offset = offset;

    public ref T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref GetRef();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetRef() => ref memory.Span[offset];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly T GetReadOnlyRef() => ref memory.Span[offset];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Memory<T> UnsafeGetMemory() => memory;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int UnsafeGetOffset() => offset;

    #region Equals

    public bool Equals(MemoryRef<T> other) => memory.Equals(other.memory) && offset == other.offset;

    public override bool Equals(object? obj) => obj is MemoryRef<T> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(memory, offset);

    public static bool operator ==(MemoryRef<T> left, MemoryRef<T> right) => left.Equals(right);

    public static bool operator !=(MemoryRef<T> left, MemoryRef<T> right) => !left.Equals(right);

    #endregion
}

public readonly struct ReadOnlyMemoryRef<T>(ReadOnlyMemory<T> memory, int offset) :
    IEquatable<ReadOnlyMemoryRef<T>>, IReadOnlyRef<T>
{
    private readonly ReadOnlyMemory<T> memory = memory;
    private readonly int offset = offset;

    public ref readonly T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref GetReadOnlyRef();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly T GetReadOnlyRef() => ref memory.Span[offset];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlyMemory<T> UnsafeGetMemory() => memory;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int UnsafeGetOffset() => offset;

    #region Equals

    public bool Equals(ReadOnlyMemoryRef<T> other) => memory.Equals(other.memory) && offset == other.offset;

    public override bool Equals(object? obj) => obj is ReadOnlyMemoryRef<T> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(memory, offset);

    public static bool operator ==(ReadOnlyMemoryRef<T> left, ReadOnlyMemoryRef<T> right) => left.Equals(right);

    public static bool operator !=(ReadOnlyMemoryRef<T> left, ReadOnlyMemoryRef<T> right) => !left.Equals(right);

    #endregion
}
