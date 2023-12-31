﻿#if NET8_0_OR_GREATER

using BetterCollections.Cryptography;
using BetterCollections.Misc;

namespace Tests;

public class TestAHasher
{
    [Test, Parallelizable]
    public void Test1()
    {
        var counts = new int[32];
        var count = 0;

        for (int k = 0; k < 100; k++)
        {
            ParallelEnumerable.Range(-500, 1000)
                .ForAll(i =>
                {
                    var a = (uint)AHasher.Combine((ulong)i);
                    var s = a.ToBinaryString();
                    var ss = s.Replace("_", "");
                    Interlocked.Increment(ref count);
                    for (var j = 0; j < 32; j++)
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
        var counts = new int[32];
        var count = 0;

        for (int k = 0; k < 100; k++)
        {
            ParallelEnumerable.Range(-500, 1000)
                .ForAll(i =>
                {
                    var a = HashCode.Combine(i);
                    var s = ((uint)a).ToBinaryString();
                    var ss = s.Replace("_", "");
                    Interlocked.Increment(ref count);
                    for (var j = 0; j < 32; j++)
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

#endif
