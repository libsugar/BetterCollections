using System.Buffers;
using System.Runtime.CompilerServices;

namespace BetterCollections.Buffers;

public abstract class ArrayPoolFactory
{
    public static ArrayPoolFactory Shared { get; } = new SharedArrayPoolFactory();

    public static ArrayPoolFactory DirectAllocation { get; } = new DirectAllocationArrayPoolFactory();

    public abstract bool MustReturn
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public abstract ArrayPool<T> Get<T>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public abstract ArrayPool<T> GetMayUninitialized<T>();

    private sealed class SharedArrayPoolFactory : ArrayPoolFactory
    {
        public override bool MustReturn
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override ArrayPool<T> Get<T>() => ArrayPool<T>.Shared;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override ArrayPool<T> GetMayUninitialized<T>() => ArrayPool<T>.Shared;
    }

    private sealed class DirectAllocationArrayPoolFactory : ArrayPoolFactory
    {
        public override bool MustReturn
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override ArrayPool<T> Get<T>() => DirectAllocationArrayPool<T>.Instance;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override ArrayPool<T> GetMayUninitialized<T>() => DirectAllocationUninitializedArrayPool<T>.Instance;
    }
}
