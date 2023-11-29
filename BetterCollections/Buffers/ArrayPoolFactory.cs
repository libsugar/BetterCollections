using System.Buffers;

namespace BetterCollections.Buffers;

public abstract class ArrayPoolFactory
{
    public static ArrayPoolFactory Shared { get; } = new SharedArrayPoolFactory();
    
    public static ArrayPoolFactory DirectAllocation { get; } = new DirectAllocationArrayPoolFactory();

    public abstract ArrayPool<T> Get<T>();

    private class SharedArrayPoolFactory : ArrayPoolFactory
    {
        public override ArrayPool<T> Get<T>() => ArrayPool<T>.Shared;
    }

    private class DirectAllocationArrayPoolFactory : ArrayPoolFactory
    {
        public override ArrayPool<T> Get<T>() => DirectAllocationArrayPool<T>.Instance;
    }
}
