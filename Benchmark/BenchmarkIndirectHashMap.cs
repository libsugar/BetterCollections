using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BetterCollections;

namespace Benchmark;

[MemoryDiagnoser]
[JitStatsDiagnoser]
[DisassemblyDiagnoser]
public class BenchmarkIndirectHashMap
{
    [Benchmark]
    public IndirectHashMap<int, int> FlatHashMap()
    {
        var map = new IndirectHashMap<int, int>();
        for (var i = 0; i < 2; i++)
        {
            map.Add(i, i);
        }
        return map;
    }
}

