using System.Runtime.CompilerServices;
using BetterCollections;

namespace Tests;

public class TestIndirectHashMap
{
#if NET7_0_OR_GREATER
    [Test]
    public void TestAdd1()
    {
        var map = new IndirectHashMap<int, int>();
        for (var i = 0; i < 20; i++)
        {
            map.Add(i, i);
        }
        Console.WriteLine(map);
    }
#endif
}
