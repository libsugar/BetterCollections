using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BetterCollections;

namespace Benchmark;

[MemoryDiagnoser]
[JitStatsDiagnoser]
[DisassemblyDiagnoser]
public class BenchmarkHashMap_Add1000
{
    [Benchmark]
    public IndirectHashMap<int, int> IndirectHashMap()
    {
        var map = new IndirectHashMap<int, int>();
        for (var i = 0; i < 1000; i++)
        {
            map.Add(i, i);
        }
        return map;
    }

    [Benchmark]
    public FlatHashMap<int, int> FlatHashMap()
    {
        var map = new FlatHashMap<int, int>();
        for (var i = 0; i < 1000; i++)
        {
            map.Add(i, i);
        }
        return map;
    }

    [Benchmark(Baseline = true)]
    public Dictionary<int, int> Dictionary()
    {
        var map = new Dictionary<int, int>();
        for (var i = 0; i < 1000; i++)
        {
            map.Add(i, i);
        }
        return map;
    }
}

[MemoryDiagnoser]
[JitStatsDiagnoser]
[DisassemblyDiagnoser]
public class BenchmarkHashMap_Add20
{
    [Benchmark]
    public IndirectHashMap<int, int> IndirectHashMap()
    {
        var map = new IndirectHashMap<int, int>();
        for (var i = 0; i < 20; i++)
        {
            map.Add(i, i);
        }
        return map;
    }

    [Benchmark]
    public FlatHashMap<int, int> FlatHashMap()
    {
        var map = new FlatHashMap<int, int>();
        for (var i = 0; i < 20; i++)
        {
            map.Add(i, i);
        }
        return map;
    }

    [Benchmark(Baseline = true)]
    public Dictionary<int, int> Dictionary()
    {
        var map = new Dictionary<int, int>();
        for (var i = 0; i < 20; i++)
        {
            map.Add(i, i);
        }
        return map;
    }
}

[MemoryDiagnoser]
[JitStatsDiagnoser]
[DisassemblyDiagnoser]
public class BenchmarkHashMap_Add2
{
    [Benchmark]
    public IndirectHashMap<int, int> IndirectHashMap()
    {
        var map = new IndirectHashMap<int, int>();
        for (var i = 0; i < 2; i++)
        {
            map.Add(i, i);
        }
        return map;
    }

    [Benchmark]
    public FlatHashMap<int, int> FlatHashMap()
    {
        var map = new FlatHashMap<int, int>();
        for (var i = 0; i < 2; i++)
        {
            map.Add(i, i);
        }
        return map;
    }

    [Benchmark(Baseline = true)]
    public Dictionary<int, int> Dictionary()
    {
        var map = new Dictionary<int, int>();
        for (var i = 0; i < 2; i++)
        {
            map.Add(i, i);
        }
        return map;
    }
}

[MemoryDiagnoser]
[JitStatsDiagnoser]
[DisassemblyDiagnoser]
public class BenchmarkHashMap_Get1000
{
    private IndirectHashMap<int, int> indirectHashMap = new();
    private FlatHashMap<int, int> flatHashMap = new();
    private Dictionary<int, int> dictionary = new();

    [GlobalSetup]
    public void Setup()
    {
        for (var i = 0; i < 2; i++)
        {
            indirectHashMap.Add(i, i);
        }

        for (var i = 0; i < 1000; i++)
        {
            flatHashMap.Add(i, i);
        }

        for (var i = 0; i < 1000; i++)
        {
            dictionary.Add(i, i);
        }
    }

    [Benchmark]
    public int IndirectHashMap()
    {
        var a = 0;
        for (var i = 0; i < 1000; i++)
        {
            a += indirectHashMap[i];
        }
        return a;
    }

    [Benchmark]
    public int FlatHashMap()
    {
        var a = 0;
        for (var i = 0; i < 1000; i++)
        {
            a += flatHashMap[i];
        }
        return a;
    }

    [Benchmark(Baseline = true)]
    public int Dictionary()
    {
        var a = 0;
        for (var i = 0; i < 1000; i++)
        {
            a += dictionary[i];
        }
        return a;
    }
}

[MemoryDiagnoser]
[JitStatsDiagnoser]
[DisassemblyDiagnoser]
public class BenchmarkHashMap_Add10000
{
    [Benchmark]
    public IndirectHashMap<int, int> IndirectHashMap()
    {
        var map = new IndirectHashMap<int, int>();
        for (var i = 0; i < 10000; i++)
        {
            map.Add(i, i);
        }
        return map;
    }

    [Benchmark]
    public FlatHashMap<int, int> FlatHashMap()
    {
        var map = new FlatHashMap<int, int>();
        for (var i = 0; i < 10000; i++)
        {
            map.Add(i, i);
        }
        return map;
    }

    [Benchmark(Baseline = true)]
    public Dictionary<int, int> Dictionary()
    {
        var map = new Dictionary<int, int>();
        for (var i = 0; i < 10000; i++)
        {
            map.Add(i, i);
        }
        return map;
    }
}
