using BetterCollections.Cryptography;
using BetterCollections.Misc;

namespace Tests;

public class TestAHasher
{
    [Test, Repeat(10), Parallelizable]
    public void Test1()
    {
        var counts = new int[64];

        for (int k = 0; k < 100; k++)
        {
            var hasher = new AHasher();
            ParallelEnumerable.Range(0, 100)
                .ForAll(_ =>
                {
                    var rand = Random.Shared;
                    var a = hasher.Hash((ulong)rand.NextInt64());
                    var s = a.ToBinaryString();
                    var ss = s.Replace("_", "");
                    for (var j = 0; j < 64; j++)
                    {
                        if (ss[j] == '1') Interlocked.Increment(ref counts[j]);
                    }
                });
        }

        Console.WriteLine();
        Console.WriteLine(string.Join(", ", counts.Select(a => a / 10000d)));
        Console.WriteLine();
        var ave = counts.Average(a => a / 10000d);
        Console.WriteLine($"Average: {ave * 100} %");
        var round = Math.Round(ave * 100);
        Console.WriteLine($"Rounded: {round} %");

        Assert.That(Math.Abs(Math.Round(ave * 10) - 5), Is.LessThan(0.1d));
    }
}
