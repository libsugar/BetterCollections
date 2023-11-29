using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BetterCollections.Memories;

public static class OffsetRef
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal sealed class RawData
    {
        public byte Data;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T UnsafeGetRef<T>(object obj, nuint offset) =>
        ref Unsafe.As<byte, T>(ref Unsafe.Add(ref Unsafe.As<RawData>(obj).Data, offset));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe OffsetRef<T> UnsafeCreate<T>(object obj, ref T addr)
    {
        var source = (nuint)Unsafe.AsPointer(ref Unsafe.As<RawData>(obj).Data);
        var target = (nuint)Unsafe.AsPointer(ref Unsafe.As<T, byte>(ref addr));
        var offset = target - source;
        return new(obj, offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe ReadOnlyOffsetRef<T> UnsafeCreateReadOnly<T>(object obj, ref readonly T addr)
    {
        var source = (nuint)Unsafe.AsPointer(ref Unsafe.As<RawData>(obj).Data);
        var target = (nuint)Unsafe.AsPointer(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in addr)));
        var offset = target - source;
        return new(obj, offset);
    }
}

public readonly struct OffsetRef<T> :
    IEquatable<OffsetRef<T>>, IRef<T>, IReadOnlyRef<T>
{
    private readonly object obj;
    private readonly nuint offset;

    internal OffsetRef(object obj, nuint offset)
    {
        this.obj = obj;
        this.offset = offset;
    }

    public ref T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref GetRef();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetRef() => ref OffsetRef.UnsafeGetRef<T>(obj, offset);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly T GetReadOnlyRef() => ref OffsetRef.UnsafeGetRef<T>(obj, offset);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public object UnsafeGetObject() => obj;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint UnsafeGetOffset() => offset;

    #region Equals

    public bool Equals(OffsetRef<T> other) => ReferenceEquals(obj, other.obj) && offset == other.offset;

    public override bool Equals(object? obj) => obj is OffsetRef<T> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(RuntimeHelpers.GetHashCode(obj), offset);

    public static bool operator ==(OffsetRef<T> left, OffsetRef<T> right) => left.Equals(right);

    public static bool operator !=(OffsetRef<T> left, OffsetRef<T> right) => !left.Equals(right);

    #endregion
}

public readonly struct ReadOnlyOffsetRef<T> :
    IEquatable<ReadOnlyOffsetRef<T>>, IReadOnlyRef<T>
{
    private readonly object obj;
    private readonly nuint offset;

    internal ReadOnlyOffsetRef(object obj, nuint offset)
    {
        this.obj = obj;
        this.offset = offset;
    }

    public ref readonly T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref GetReadOnlyRef();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly T GetReadOnlyRef() => ref OffsetRef.UnsafeGetRef<T>(obj, offset);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public object UnsafeGetObject() => obj;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint UnsafeGetOffset() => offset;

    #region Equals

    public bool Equals(ReadOnlyOffsetRef<T> other) => ReferenceEquals(obj, other.obj) && offset == other.offset;

    public override bool Equals(object? obj) => obj is ReadOnlyOffsetRef<T> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(RuntimeHelpers.GetHashCode(obj), offset);

    public static bool operator ==(ReadOnlyOffsetRef<T> left, ReadOnlyOffsetRef<T> right) => left.Equals(right);

    public static bool operator !=(ReadOnlyOffsetRef<T> left, ReadOnlyOffsetRef<T> right) => !left.Equals(right);

    #endregion
}
