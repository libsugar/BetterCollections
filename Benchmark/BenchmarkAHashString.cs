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
        var hasher = AHasher2.Global;
        hasher.AddString(Str);
        return hasher.ToHashCode();
    }

    [Benchmark]
    public int AHash_AddString_2()
    {
        var hasher = AesHasher.Create(AHasher2.GlobalRandomState);
        hasher = AesHasher.AddString(hasher, Str);
        return AesHasher.ToHashCode(hasher);
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
