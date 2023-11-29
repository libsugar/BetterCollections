using System;

namespace BetterCollections.Concurrent;

public class OnceInit<T>
{
    private T value = default!;
    private readonly object locker = new();
    private volatile bool ready;

    public T Get(Func<T> Init)
    {
        if (ready) return value;
        lock (locker)
        {
            value = Init();
            ready = true;
        }
        return value;
    }

    public T Get<A>(A arg, Func<A, T> Init)
    {
        if (ready) return value;
        lock (locker)
        {
            value = Init(arg);
            ready = true;
        }
        return value;
    }
}
