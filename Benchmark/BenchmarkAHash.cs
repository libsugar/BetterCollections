using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BetterCollections.Cryptography;

namespace Benchmark;

[MemoryDiagnoser]
[JitStatsDiagnoser]
[DisassemblyDiagnoser]
public class BenchmarkAHash
{
    private readonly AHasher AHasher = new();

    [Benchmark]
    public ulong AHash()
    {
        var r = 0ul;
        for (int i = 0; i < 1000; i++)
        {
            r += AHasher.Hash((ulong)i);
        }
        return r;
    }

    [Benchmark(Baseline = true)]
    public int HashCodeCombine()
    {
        var r = 0;
        for (int i = 0; i < 1000; i++)
        {
            r += HashCode.Combine(i);
        }
        return r;
    }
}
