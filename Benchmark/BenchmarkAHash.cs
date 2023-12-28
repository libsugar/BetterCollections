using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BetterCollections.Cryptography;
#if NET8_0_OR_GREATER
using BetterCollections.Cryptography.AHasherImpl;
#endif

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

#if NET8_0_OR_GREATER
    [Benchmark]
    public int AHash2()
    {
        var r = 0;
        for (int i = 0; i < 1000; i++)
        {
            r += AHasher2.Combine(i);
        }
        return r;
    }

    [Benchmark]
    public int AHash3()
    {
        var r = 0;
        for (int i = 0; i < 1000; i++)
        {
            var hasher = AesHasher.Create(AHasher2.GlobalRandomState);
            hasher = AesHasher.Add(hasher, i);
            r += AesHasher.ToHashCode(hasher);
        }
        return r;
    }
#endif

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
