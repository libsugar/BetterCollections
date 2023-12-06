using BetterCollections;

namespace Tests;

public class TestFlatHashMap
{
    [Test]
    public void TestAdd1()
    {
        var map = new FlatHashMap<int, int>();
        map.Add(1, 2);
        map.Add(2, 3);
    }
}
