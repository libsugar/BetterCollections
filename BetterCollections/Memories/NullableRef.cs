#if NET7_0_OR_GREATER
using System.Runtime.CompilerServices;

namespace BetterCollections.Memories;

public readonly ref struct NullableRef<T>
{
    public static NullableRef<T> Null
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new();
    }

    public readonly ref T Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NullableRef()
    {
        Value = ref Unsafe.NullRef<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NullableRef(ref T value)
    {
        Value = ref value;
    }

    public bool HasValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => !Unsafe.IsNullRef(ref Value);
    }

    public bool IsNull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Unsafe.IsNullRef(ref Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe bool Equals(NullableRef<T> other)
        => Unsafe.AsPointer(ref Value) == Unsafe.AsPointer(ref other.Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override unsafe int GetHashCode()
        => ((nuint)Unsafe.AsPointer(ref Value)).GetHashCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator NullableReadonlyRef<T>(NullableRef<T> nr)
        => new(ref nr.Value);
}

public readonly ref struct NullableReadonlyRef<T>
{
    public static NullableReadonlyRef<T> Null
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new();
    }

    public readonly ref readonly T Value;

#pragma warning disable CS8618
    public NullableReadonlyRef()
#pragma warning restore CS8618
    {
        Value = ref Unsafe.NullRef<T>();
    }

#pragma warning disable CS8618
    public NullableReadonlyRef(ref T value)
#pragma warning restore CS8618
    {
        Value = ref value;
    }

    public bool HasValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => !Unsafe.IsNullRef(ref Unsafe.AsRef(in Value));
    }

    public bool IsNull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Unsafe.IsNullRef(ref Unsafe.AsRef(in Value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe bool Equals(NullableReadonlyRef<T> other)
        => Unsafe.AsPointer(ref Unsafe.AsRef(in Value)) == Unsafe.AsPointer(ref Unsafe.AsRef(in Value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override unsafe int GetHashCode()
        => ((nuint)Unsafe.AsPointer(ref Unsafe.AsRef(in Value))).GetHashCode();
}

#endif
