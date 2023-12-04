using System.Buffers;
using System.Runtime.CompilerServices;

namespace BetterCollections.Buffers;

public abstract class ArrayPoolFactory
{
    public static ArrayPoolFactory Shared { get; } = new SharedArrayPoolFactory();

    public static ArrayPoolFactory DirectAllocation { get; } = new DirectAllocationArrayPoolFactory();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public abstract ArrayPool<T> Get<T>();

    private sealed class SharedArrayPoolFactory : ArrayPoolFactory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override ArrayPool<T> Get<T>() => ArrayPool<T>.Shared;
    }

    private sealed class DirectAllocationArrayPoolFactory : ArrayPoolFactory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override ArrayPool<T> Get<T>() => DirectAllocationArrayPool<T>.Instance;
    }
}
