using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using BetterCollections.Buffers;

namespace BetterCollections.Misc;

public abstract class AHashTable<TKey> : IDisposable
{
    protected const int ShrinkThreshold = 3;
    protected const int StartOfFreeList = -3;

    protected readonly ArrayPool<int> poolInt;
    protected readonly ArrayPool<Node> poolNode;
    protected readonly ArrayPool<TKey> poolKey;

    protected IEqualityComparer<TKey>? comparer;

    protected int[]? buckets;
    protected Node[]? nodes;

    protected int size;

    protected int freeList = -1;
    protected int freeCount = 0;

    protected int count;

    protected int version;

    /// <summary>This should only be used on 64-bit</summary>
    protected ulong fastModMultiplier;
    
    protected AHashTable(int cap, IEqualityComparer<TKey>? comparer, ArrayPoolFactory poolFactory)
    {
        poolInt = poolFactory.Get<int>();
        poolNode = poolFactory.Get<Node>();
        poolKey = poolFactory.Get<TKey>();

        comparer = EqHelpers.NormalizeEqualityComparer(comparer);

        if (cap < 0) throw new ArgumentOutOfRangeException(nameof(cap));

        if (cap > 0) Init(cap);
    }

    protected struct Node
    {
        public int hashCode;
        public int next;
        public TKey key;
    }

    #region Dispose

    protected virtual void Dispose(bool disposing)
    {
        if (buckets != null)
        {
            poolInt.Return(buckets);
            buckets = null;
        }
        if (nodes != null)
        {
            poolNode.Return(nodes, RuntimeHelpers.IsReferenceOrContainsReferences<Node>());
            nodes = null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~AHashTable() => Dispose(false);

    #endregion

    public int Count => count;

    protected int Init(int cap)
    {
        var size = this.size = HashHelpers.GetPrime(cap);

        var buckets = poolInt.Rent(size);
        var nodes = poolNode.Rent(size);
        RentDataArray(size);

        this.buckets = buckets;
        this.nodes = nodes;

        freeList = -1;
        if (Environment.Is64BitProcess)
        {
            fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)size);
        }

        return size;
    }

    protected abstract void RentDataArray(int len);

    protected abstract void ReRentDataArray(int oldLen, int newLen);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ref int GetBucketRef(int hashCode)
    {
        var buckets = this.buckets!;
        if (Environment.Is64BitProcess)
        {
            return ref buckets[HashHelpers.FastMod((uint)hashCode, (uint)buckets.Length, fastModMultiplier)];
        }
        else
        {
            return ref buckets[(uint)hashCode % buckets.Length];
        }
    }

    protected interface IExtraData
    {
        public void SetData(AHashTable<TKey> baseThis, int index);
    }

    protected bool TryInsert<E>(TKey key, E extra, InsertBehavior behavior) where E : IExtraData
    {
        if (buckets == null) Init(0);
        Debug.Assert(buckets != null);

        var nodes = this.nodes!;
        var size = this.size;
        Debug.Assert(nodes != null);

        var hashCode = EqHelpers.GetHashCode(key, comparer);
        ref var bucket = ref GetBucketRef(hashCode);

        var collisionCount = 0u;
        var i = bucket - 1;

        ref var node = ref Unsafe.NullRef<Node>();

        if (typeof(TKey).IsValueType && comparer == null)
        {
            for (;;)
            {
                if (i >= size) break;

                node = ref nodes[i];

                if (node.hashCode == hashCode && EqualityComparer<TKey>.Default.Equals(node.key, key))
                {
                    if (behavior == InsertBehavior.OverwriteIfExisting)
                    {
                        extra.SetData(this, i);
                        return true;
                    }

                    return false;
                }

                i = node.next;

                collisionCount++;

                if (collisionCount > size)
                    throw InvalidOperationException_ConcurrentOperationsNotSupported;
            }
        }
        else
        {
            Debug.Assert(comparer is not null);

            for (;;)
            {
                if (i >= size) break;

                node = ref nodes[i];

                if (node.hashCode == hashCode && comparer.Equals(node.key, key))
                {
                    if (behavior == InsertBehavior.OverwriteIfExisting)
                    {
                        extra.SetData(this, i);
                        return true;
                    }

                    return false;
                }

                i = node.next;

                collisionCount++;

                if (collisionCount > size)
                    throw InvalidOperationException_ConcurrentOperationsNotSupported;
            }
        }

        int index;
        if (freeCount > 0)
        {
            index = freeList;
            Debug.Assert((StartOfFreeList - nodes[freeList].next) >= -1,
                "shouldn't overflow because `next` cannot underflow");
            freeList = StartOfFreeList - nodes[freeList].next;
            freeCount--;
        }
        else
        {
            var count = this.count;
            if (count == size)
            {
                Resize();
                bucket = ref GetBucketRef(hashCode);
            }
            index = count;
            this.count = count + 1;
            nodes = this.nodes!;
            size = this.size;
        }

        node = ref nodes[index];
        node.hashCode = hashCode;
        node.next = bucket - 1;
        node.key = key;
        extra.SetData(this, index);

        version++;

        return true;
    }

    protected void Resize() => Resize(HashHelpers.ExpandPrime(count));

    protected void Resize(int newSize)
    {
        var oldSize = size;
        var oldNodes = nodes;

        Debug.Assert(oldNodes != null, "_entries should be non-null");
        Debug.Assert(newSize >= oldSize);

        var newNodes = poolNode.Rent(newSize);
        var newBuckets = poolInt.Rent(newSize);
        oldNodes.AsSpan(0, oldSize).CopyTo(newNodes.AsSpan(0, oldSize));
        poolNode.Return(oldNodes, RuntimeHelpers.IsReferenceOrContainsReferences<Node>());
        ReRentDataArray(oldSize, newSize);
        nodes = newNodes;
        buckets = newBuckets;

        if (Environment.Is64BitProcess)
        {
            fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)newSize);
        }

        for (var i = 0; i < count; i++)
        {
            ref var node = ref nodes[i];
            if (node.next >= -1)
            {
                ref var bucket = ref GetBucketRef(node.hashCode);
                bucket = i + 1;
            }
        }
    }

    protected static InvalidOperationException InvalidOperationException_ConcurrentOperationsNotSupported => new(
        "Operations that change non-concurrent collections must have exclusive access. A concurrent update was performed on this collection and corrupted its state. The collection's state is no longer correct."
    );
}
