using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BetterCollections.Buffers;
using BetterCollections.Memories;

namespace BetterCollections;

/// <summary>Optional pooled List, from which the original array can be obtained</summary>
public class Vec<T> : IList<T>, IReadOnlyList<T>, IDisposable
{
    protected readonly ArrayPool<T> pool;
    private T[] array;
    private int size;

    protected const int DefaultCapacity = 4;

    public Vec() : this(0) { }

    public Vec(int cap) : this(cap, DirectAllocationArrayPool<T>.Instance) { }

    public Vec(ArrayPool<T> pool) : this(0, pool) { }

    public Vec(int cap, ArrayPool<T> pool)
    {
        if (cap < 0) throw new ArgumentOutOfRangeException(nameof(cap));
        this.pool = pool;
        array = pool.Rent(cap);
        size = 0;
    }

    public int Count => size;

    public bool IsReadOnly => false;

    public bool IsEmpty => size == 0;

    public Memory<T> AsMemory => array.AsMemory(0, size);
    public Span<T> AsSpan => array.AsSpan(0, size);

    public T this[int index]
    {
        get => AsSpan[index];
        set => AsSpan[index] = value;
    }

    public T[] UnsafeArray => array;

    public ArrayRef<T> UnsafeGetArrayRef(int index) => new(array, index);
    public OffsetRef<T> UnsafeGetOffsetRef(int index) => OffsetRef.UnsafeCreate(array, ref array[index]);

    public ref T UnsafeGetRef(int index) => ref array[index];

    #region Enumerator

    public RefMemoryEnumerator<T> GetEnumerator() => AsMemory.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new MemoryEnumeratorClass<T>(AsMemory);

    IEnumerator IEnumerable.GetEnumerator() => new MemoryEnumeratorClass<T>(AsMemory);

    #endregion

    public void Add(T item)
    {
        if (size >= array.Length) Grow();
        array[size] = item;
        size += 1;
    }

    private void Grow()
    {
        var old_array = array;
        if (array.Length == 0)
        {
            array = pool.Rent(DefaultCapacity);
            pool.Return(old_array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
        }
        else
        {
            var new_array = NewGrown();
            try
            {
                AsSpan.CopyTo(new_array);
            }
            catch
            {
                pool.Return(new_array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                throw;
            }
            array = new_array;
            pool.Return(old_array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
        }
    }

    private T[] NewGrown() => pool.Rent(array.Length * 2);

    public void Clear()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            if (size > 0)
            {
                Array.Clear(array, 0, size);
            }
        }
        size = 0;
    }

    public bool Contains(T item) => !IsEmpty && IndexOf(item) >= 0;

    public void CopyTo(T[] array, int arrayIndex) => AsSpan.CopyTo(array.AsSpan(arrayIndex));

    public bool Remove(T item)
    {
        var index = IndexOf(item);
        if (index < 0) return false;
        RemoveAt(index);
        return true;
    }

    public int IndexOf(T item) => Array.IndexOf(array, item, 0, size);

    public void Insert(int index, T item)
    {
        if (index < 0 || index > size) throw new ArgumentOutOfRangeException(nameof(index));
        if (index == size)
        {
            Add(item);
            return;
        }
        if (size >= array.Length)
        {
            var old_array = array;
            var new_array = NewGrown();
            try
            {
                var old_span = old_array.AsSpan(0, size);
                var new_span = new_array.AsSpan(0, size + 1);
                old_span[..index].CopyTo(new_span);
                var i1 = index + 1;
                old_span[index..].CopyTo(new_span[i1..]);
                new_span[index] = item;
            }
            catch
            {
                pool.Return(new_array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                throw;
            }

            array = new_array;
            size += 1;
            pool.Return(old_array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
        }
        else
        {
            size += 1;
            var span = AsSpan;
            var i1 = index + 1;
            span[index..^1].CopyTo(span[i1..]);
            span[index] = item;
        }
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= size) throw new ArgumentOutOfRangeException(nameof(index));
        var span = AsSpan;
        size -= 1;
        var i1 = index + 1;
        span[i1..].CopyTo(span[index..]);
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            span[size] = default!;
        }
    }

    public T[] ToArray() => AsSpan.ToArray();

    public override string ToString()
    {
        if (typeof(T) == typeof(char)) return AsSpan.ToString();
        return $"Vec<{typeof(T).Name}>[{size}]";
    }

    #region Dispose

    protected virtual void Dispose(bool disposing)
    {
        if (array != null!)
        {
            pool.Return(array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
            array = null!;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~Vec() => Dispose(false);

    #endregion
}
