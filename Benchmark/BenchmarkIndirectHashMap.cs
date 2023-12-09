using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BetterCollections;

namespace Benchmark;

[MemoryDiagnoser]
[JitStatsDiagnoser]
[DisassemblyDiagnoser]
public class BenchmarkIndirectHashMap
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void DoAdd(IndirectHashMap<int, int> map, int i)
    {
        map.Add(i, i);
    }

    [Benchmark]
    public IndirectHashMap<int, int> IndirectHashMap()
    {
        var map = new IndirectHashMap<int, int>();
        for (var i = 0; i < 2; i++)
        {
            DoAdd(map, i);
        }
        return map;
    }
}
