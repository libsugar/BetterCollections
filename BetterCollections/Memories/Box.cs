using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BetterCollections.Memories;

public static class Box
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Box<T> Make<T>(T value) => new(value);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlyBox<T> MakeReadOnly<T>(T value) => new(value);
}

public static class BoxEx
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Box<T> Box<T>(this T value) => new(value);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlyBox<T> ReadOnlyBox<T>(this T value) => new(value);
}

public class Box<T>(T Value) : IRef<T>, IReadOnlyRef<T>, IStrongBox,
    IEquatable<T>, IEquatable<Box<T>>,
    IComparable<T>, IComparable<Box<T>>
{
    public T Value = Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetRef() => ref Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly T GetReadOnlyRef() => ref Value;

    #region ToString

    public override string ToString() => $"{Value}";

    #endregion

    #region Equals

    public bool Equals(T? other) => EqualityComparer<T>.Default.Equals(Value, other!);

    public bool Equals(Box<T>? other) => !ReferenceEquals(other, null) && Equals(other.Value);


    public override bool Equals(object? obj) => obj is Box<T> box ? Equals(box) : obj is T v && Equals(v);

    // ReSharper disable once NonReadonlyMemberInGetHashCode
    public override int GetHashCode() => Value?.GetHashCode() ?? 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Box<T> left, Box<T> right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Box<T> left, Box<T> right) => !left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Box<T> left, T right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Box<T> left, T right) => !left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(T left, Box<T> right) => right.Equals(left);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(T left, Box<T> right) => !right.Equals(left);

    public int CompareTo(T? other) => Comparer<T>.Default.Compare(Value, other!);

    public int CompareTo(Box<T>? other) => ReferenceEquals(other, null) ? -1 : CompareTo(other.Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(Box<T> left, Box<T> right) => left.CompareTo(right) < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(Box<T> left, Box<T> right) => left.CompareTo(right) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(Box<T> left, Box<T> right) => left.CompareTo(right) <= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(Box<T> left, Box<T> right) => left.CompareTo(right) >= 0;

    #endregion

    #region IStrongBox

    object? IStrongBox.Value
    {
        get => Value;
        set => Value = (T)value!;
    }

    #endregion
}

public class ReadOnlyBox<T>(T Value) : IReadOnlyRef<T>,
    IEquatable<T>, IEquatable<ReadOnlyBox<T>>,
    IComparable<T>, IComparable<ReadOnlyBox<T>>
{
    public readonly T Value = Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly T GetReadOnlyRef() => ref Value;

    #region ToString

    public override string ToString() => $"{Value}";

    #endregion

    #region Equals

    public bool Equals(T? other) => EqualityComparer<T>.Default.Equals(Value, other!);

    public bool Equals(ReadOnlyBox<T>? other) => !ReferenceEquals(other, null) && Equals(other.Value);


    public override bool Equals(object? obj) => obj is ReadOnlyBox<T> box ? Equals(box) : obj is T v && Equals(v);

    // ReSharper disable once NonReadonlyMemberInGetHashCode
    public override int GetHashCode() => Value?.GetHashCode() ?? 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(ReadOnlyBox<T> left, ReadOnlyBox<T> right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(ReadOnlyBox<T> left, ReadOnlyBox<T> right) => !left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(ReadOnlyBox<T> left, T right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(ReadOnlyBox<T> left, T right) => !left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(T left, ReadOnlyBox<T> right) => right.Equals(left);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(T left, ReadOnlyBox<T> right) => !right.Equals(left);

    public int CompareTo(T? other) => Comparer<T>.Default.Compare(Value, other!);

    public int CompareTo(ReadOnlyBox<T>? other) => ReferenceEquals(other, null) ? -1 : CompareTo(other.Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(ReadOnlyBox<T> left, ReadOnlyBox<T> right) => left.CompareTo(right) < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(ReadOnlyBox<T> left, ReadOnlyBox<T> right) => left.CompareTo(right) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(ReadOnlyBox<T> left, ReadOnlyBox<T> right) => left.CompareTo(right) <= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(ReadOnlyBox<T> left, ReadOnlyBox<T> right) => left.CompareTo(right) >= 0;

    #endregion
}
