using System;
using System.Runtime.CompilerServices;

namespace BetterCollections.Memories;

public readonly struct ArrayRef<T>(T[] array, int offset) :
    IEquatable<ArrayRef<T>>, IRef<T>, IReadOnlyRef<T>
{
    private readonly T[] array = array;
    private readonly int offset = offset;

    public ref T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref GetRef();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetRef() => ref array[offset];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly T GetReadOnlyRef() => ref array[offset];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T[] UnsafeGetArray() => array;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int UnsafeGetOffset() => offset;

    #region Equals

    public bool Equals(ArrayRef<T> other) => ReferenceEquals(array, other.array) && offset == other.offset;

    public override bool Equals(object? obj) => obj is ArrayRef<T> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(array, offset);

    public static bool operator ==(ArrayRef<T> left, ArrayRef<T> right) => left.Equals(right);

    public static bool operator !=(ArrayRef<T> left, ArrayRef<T> right) => !left.Equals(right);

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlyArrayRef<T>(ArrayRef<T> self) => new(self.array, self.offset);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator MemoryRef<T>(ArrayRef<T> self) => new(self.array, self.offset);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlyMemoryRef<T>(ArrayRef<T> self) => new(self.array, self.offset);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator OffsetRef<T>(ArrayRef<T> self) =>
        OffsetRef.UnsafeCreate(self.array, ref self.Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlyOffsetRef<T>(ArrayRef<T> self) =>
        OffsetRef.UnsafeCreateReadOnly(self.array, ref self.Value);
}

public readonly struct ReadOnlyArrayRef<T>(T[] array, int offset) :
    IEquatable<ReadOnlyArrayRef<T>>, IReadOnlyRef<T>
{
    private readonly T[] array = array;
    private readonly int offset = offset;

    public ref readonly T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref GetReadOnlyRef();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly T GetReadOnlyRef() => ref array[offset];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T[] UnsafeGetArray() => array;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int UnsafeGetOffset() => offset;

    #region Equals

    public bool Equals(ReadOnlyArrayRef<T> other) => ReferenceEquals(array, other.array) && offset == other.offset;

    public override bool Equals(object? obj) => obj is ReadOnlyArrayRef<T> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(array, offset);

    public static bool operator ==(ReadOnlyArrayRef<T> left, ReadOnlyArrayRef<T> right) => left.Equals(right);

    public static bool operator !=(ReadOnlyArrayRef<T> left, ReadOnlyArrayRef<T> right) => !left.Equals(right);

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlyOffsetRef<T>(ReadOnlyArrayRef<T> self) =>
        OffsetRef.UnsafeCreateReadOnly(self.array, in self.Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlyMemoryRef<T>(ReadOnlyArrayRef<T> self) => new(self.array, self.offset);
}
