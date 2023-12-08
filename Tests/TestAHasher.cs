using BetterCollections.Cryptography;
using BetterCollections.Misc;

namespace Tests;

public class TestAHasher
{
    [Test, Parallelizable]
    public void Test1()
    {
        var counts = new int[64];
        var count = 0;

        for (int k = 0; k < 100; k++)
        {
            var hasher = new AHasher();
            ParallelEnumerable.Range(-500, 1000)
                .ForAll(i =>
                {
                    var a = hasher.Hash((ulong)i);
                    var s = a.ToBinaryString();
                    var ss = s.Replace("_", "");
                    Interlocked.Increment(ref count);
                    for (var j = 0; j < 64; j++)
                    {
                        if (ss[j] == '1') Interlocked.Increment(ref counts[j]);
                    }
                });
        }

        Console.WriteLine();
        Console.WriteLine(count);
        Console.WriteLine(string.Join(", ", counts.Select(a => a / (double)count)));
        Console.WriteLine();
        var ave = counts.Average(a => a / (double)count);
        Console.WriteLine($"Average: {ave * 100} %");
        var round = Math.Round(ave * 100);
        Console.WriteLine($"Rounded: {round} %");

        Assert.That(Math.Abs(Math.Round(ave * 10) - 5), Is.LessThan(0.1d));
    }
    
    [Test, Parallelizable]
    public void TestSystemHash()
    {
        var counts = new int[64];
        var count = 0;

        for (int k = 0; k < 100; k++)
        {
            ParallelEnumerable.Range(-500, 1000)
                .ForAll(i =>
                {
                    var a = HashCode.Combine(i);
                    var s = ((ulong)a).ToBinaryString();
                    var ss = s.Replace("_", "");
                    Interlocked.Increment(ref count);
                    for (var j = 0; j < 64; j++)
                    {
                        if (ss[j] == '1') Interlocked.Increment(ref counts[j]);
                    }
                });
        }

        Console.WriteLine();
        Console.WriteLine(count);
        Console.WriteLine(string.Join(", ", counts.Select(a => a / (double)count)));
        Console.WriteLine();
        var ave = counts.Average(a => a / (double)count);
        Console.WriteLine($"Average: {ave * 100} %");
        var round = Math.Round(ave * 100);
        Console.WriteLine($"Rounded: {round} %");

        Assert.That(Math.Abs(Math.Round(ave * 10) - 5), Is.LessThan(0.1d));
    }
}
