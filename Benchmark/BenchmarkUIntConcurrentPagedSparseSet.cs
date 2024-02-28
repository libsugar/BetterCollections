using System.Collections.Concurrent;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BetterCollections.Concurrent;

namespace Benchmark;

[MemoryDiagnoser]
[JitStatsDiagnoser]
[DisassemblyDiagnoser]
public class BenchmarkUIntConcurrentPagedSparseSetAdd1000
{
    [Benchmark(Baseline = true)]
    public UIntConcurrentPagedSparseSet UIntConcurrentPagedSparseSet()
    {
        var set = new UIntConcurrentPagedSparseSet();
        for (uint i = 0; i < 1000; i++)
        {
            set.Add(i);
        }
        return set;
    }

    [Benchmark]
    public ConcurrentDictionary<uint, uint> ConcurrentDictionary()
    {
        var set = new ConcurrentDictionary<uint, uint>();
        for (uint i = 0; i < 1000; i++)
        {
            set.TryAdd(i, i);
        }
        return set;
    }

    [Benchmark]
    public HashSet<uint> HashSet()
    {
        var set = new HashSet<uint>();
        for (uint i = 0; i < 1000; i++)
        {
            set.Add(i);
        }
        return set;
    }

    [Benchmark]
    public ConcurrentBag<uint> ConcurrentBag()
    {
        var set = new ConcurrentBag<uint>();
        for (uint i = 0; i < 1000; i++)
        {
            set.Add(i);
        }
        return set;
    }
}

[MemoryDiagnoser]
[JitStatsDiagnoser]
[DisassemblyDiagnoser]
public class BenchmarkUIntConcurrentPagedSparseSetAdd10000MultiThread
{
    [Benchmark(Baseline = true)]
    public UIntConcurrentPagedSparseSet UIntConcurrentPagedSparseSet()
    {
        var set = new UIntConcurrentPagedSparseSet();
        ParallelEnumerable.Range(0, 10000).ForAll(i => { set.Add((uint)i); });
        return set;
    }

    [Benchmark]
    public ConcurrentDictionary<uint, uint> ConcurrentDictionary()
    {
        var set = new ConcurrentDictionary<uint, uint>();
        ParallelEnumerable.Range(0, 10000).ForAll(i => { set.TryAdd((uint)i, 0); });
        return set;
    }

    [Benchmark]
    public ConcurrentBag<uint> ConcurrentBag()
    {
        var set = new ConcurrentBag<uint>();
        ParallelEnumerable.Range(0, 10000).ForAll(i => { set.Add((uint)i); });
        return set;
    }
}

#if NET8_0_OR_GREATER

[MemoryDiagnoser]
[JitStatsDiagnoser]
[DisassemblyDiagnoser]
public class BenchmarkUIntConcurrentPagedSparseSetAdd10000MultiThreadRandom
{
    private uint[] data;

    [GlobalSetup]
    public void Setup()
    {
        data = new uint[10000];
        ParallelEnumerable.Range(0, 10000).ForAll(i => { data[i] = (uint)i; });
        Random.Shared.Shuffle(data);
    }

    [Benchmark(Baseline = true)]
    public UIntConcurrentPagedSparseSet UIntConcurrentPagedSparseSet()
    {
        var set = new UIntConcurrentPagedSparseSet();
        data.AsParallel().ForAll(i => { set.Add((uint)i); });
        return set;
    }

    [Benchmark]
    public ConcurrentDictionary<uint, uint> ConcurrentDictionary()
    {
        var set = new ConcurrentDictionary<uint, uint>();
        data.AsParallel().ForAll(i => { set.TryAdd((uint)i, 0); });
        return set;
    }

    [Benchmark]
    public ConcurrentBag<uint> ConcurrentBag()
    {
        var set = new ConcurrentBag<uint>();
        data.AsParallel().ForAll(i => { set.Add((uint)i); });
        return set;
    }
}

#endif
