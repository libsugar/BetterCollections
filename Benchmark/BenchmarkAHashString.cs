using System.Runtime.InteropServices;
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
public class BenchmarkAHashString
{
    [Params("asd", "1234567890", "1234567890_1234567890_1234567890_1234567890",
        "1234567890_1234567890_1234567890_1234567890_1234567890_1234567890_1234567890_1234567890")]
    public string Str;

#if NET8_0_OR_GREATER
    [Benchmark]
    public int AHash_AddString_1()
    {
        var hasher = AHasher.Global;
        hasher.AddString(Str);
        return hasher.ToHashCode();
    }

    [Benchmark]
    public int AHash_AddString_2()
    {
        var hasher = AesHasher.Create(AHasher.GlobalRandomState);
        hasher = AesHasher.AddString(hasher, Str);
        return AesHasher.ToHashCode(hasher);
    }
    
    [Benchmark]
    public int Soft_AddString1()
    {
        var r = 0;
        for (int i = 0; i < 1000; i++)
        {
            var hasher = SoftHasher.Create(AHasher.GlobalRandomState);
            hasher = SoftHasher.AddString(hasher, Str);
            r += SoftHasher.ToHashCode(hasher);
        }
        return r;
    }
#endif

    [Benchmark(Baseline = true)]
    public int HashCode_AddBytes()
    {
        var hasher = new HashCode();
        hasher.AddBytes(MemoryMarshal.AsBytes(Str.AsSpan()));
        return hasher.ToHashCode();
    }
}
