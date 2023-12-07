using System.Runtime.CompilerServices;
using BetterCollections;
using System.Linq;

namespace Tests;

public class TestFlatHashMap
{
    [Test]
    public void TestAdd1()
    {
        var map = new FlatHashMap<int, int>();
        for (var i = 0; i < 1000; i++)
        {
            map.Add(i, i);
        }
        var arr = map.Select(a => a.Key).Distinct().ToList();

        Console.WriteLine(arr.Count);

        Assert.That(arr.Count, Is.EqualTo(1000));

        Assert.Multiple(() =>
        {
            ParallelEnumerable.Range(0, 1000)
                .ForAll(i =>
                {
                    var has = map.ContainsKey(i);
                    Assert.That(has, Is.True);
                });
        });
    }
}
