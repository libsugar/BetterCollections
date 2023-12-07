using BetterCollections;

for (int n = 0; n < 10000; n++)
{
    var map = new FlatHashMap<int, int>(2000);
    for (int i = 0; i < 1000; i++)
    {
        map.Add(i, i);
    }

    Console.WriteLine(map.Count);
}
