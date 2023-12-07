using BenchmarkDotNet.Attributes;
using BetterCollections;

namespace Benchmark;

[MemoryDiagnoser]
[RPlotExporter]
public class BenchmarkFlatHashMap_Add1000
{
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
[RPlotExporter]
public class BenchmarkFlatHashMap_Get1000
{
    private FlatHashMap<int, int> flatHashMap = new();
    private Dictionary<int, int> dictionary = new();

    [GlobalSetup]
    public void Setup()
    {
        flatHashMap = new FlatHashMap<int, int>();
        for (var i = 0; i < 1000; i++)
        {
            flatHashMap.Add(i, i);
        }

        dictionary = new Dictionary<int, int>();
        for (var i = 0; i < 1000; i++)
        {
            dictionary.Add(i, i);
        }
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
