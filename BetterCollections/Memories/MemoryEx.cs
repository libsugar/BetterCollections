using System;
using System.Collections;
using System.Collections.Generic;

namespace BetterCollections.Memories;

public static class MemoryEx
{
    public static RefReadOnlyMemoryEnumerator<T> GetEnumerator<T>(this ReadOnlyMemory<T> mem) => new(mem.Span);
    public static RefMemoryEnumerator<T> GetEnumerator<T>(this Memory<T> mem) => new(mem.Span);

    public static MemoryEnumerable<T> Iter<T>(this ReadOnlyMemory<T> mem) => new(mem);
    public static MemoryEnumerable<T> Iter<T>(this Memory<T> mem) => new(mem);
}

public readonly struct MemoryEnumerable<T>(ReadOnlyMemory<T> mem) : IEnumerable<T>
{
    public MemoryEnumerator<T> GetEnumerator() => new(mem);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new MemoryEnumeratorClass<T>(mem);

    IEnumerator IEnumerable.GetEnumerator() => new MemoryEnumeratorClass<T>(mem);
}

public struct MemoryEnumerator<T>(ReadOnlyMemory<T> mem) : IEnumerator<T>
{
    public void Dispose() { }

    public bool MoveNext()
    {
        if (index >= mem.Length) return false;
        index++;
        return true;
    }

    public void Reset()
    {
        index = 0;
    }

    private int index;
    public T Current => mem.Span[index - 1];

    object IEnumerator.Current => Current!;
}

public sealed class MemoryEnumeratorClass<T>(ReadOnlyMemory<T> mem) : IEnumerator<T>
{
    public void Dispose() { }

    public bool MoveNext()
    {
        if (index >= mem.Length) return false;
        index++;
        return true;
    }

    public void Reset()
    {
        index = 0;
    }

    private int index;
    public T Current => mem.Span[index - 1];

    object IEnumerator.Current => Current!;
}

public ref struct RefReadOnlyMemoryEnumerator<T>(ReadOnlySpan<T> mem)
{
    private readonly ReadOnlySpan<T> mem = mem;

    public bool MoveNext()
    {
        if (index >= mem.Length) return false;
        index++;
        return true;
    }

    public void Reset()
    {
        index = 0;
    }

    private int index;
    public ref readonly T Current => ref mem[index - 1];
}

public ref struct RefMemoryEnumerator<T>(Span<T> mem)
{
    private readonly Span<T> mem = mem;

    public bool MoveNext()
    {
        if (index >= mem.Length) return false;
        index++;
        return true;
    }

    public void Reset()
    {
        index = 0;
    }

    private int index;
    public ref T Current => ref mem[index - 1];
}
