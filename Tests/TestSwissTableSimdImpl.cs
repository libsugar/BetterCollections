#if NET7_0_OR_GREATER
using System.Runtime.Intrinsics;
using BetterCollections.Misc;

namespace Tests;

public class TestSwissTableSimdImpl : ASwissTable
{
    [Test]
    public unsafe void TestMatchH2_1()
    {
        var group = 0b0000_0001__1111_1111__0000_0001__0000_0000__0111_1110__0000_0000__1000_0000__0000_0000UL;
        var h2 = Vector64.Create((byte)0b0000_0001);
        var match = MatchH2(*(Vector64<byte>*)&group, h2);

        Console.WriteLine($"h2   : {new MatchBits(*(ulong*)&h2)}");
        Console.WriteLine($"group: {new MatchBits(group)}");
        Console.WriteLine($"match: {match}");

        var items = new List<uint>();
        foreach (var offset in match)
        {
            items.Add(offset);
            Console.WriteLine($"nth  : {offset}");
        }

        Assert.Multiple(() =>
        {
            Assert.That(items.Count, Is.EqualTo(2));
            Assert.That(items[0], Is.EqualTo(5));
            Assert.That(items[1], Is.EqualTo(7));
        });
    }

    [Test]
    public unsafe void TestMatchEmpty1()
    {
        var group = 0b0000_0000__0000_0001__1111_1111__1111_1111__0000_0000__0111_1110__1000_0000__0000_0000UL;
        var match = MatchEmpty(*(Vector64<byte>*)&group);

        Console.WriteLine($"group: {new MatchBits(group)}");
        Console.WriteLine($"match: {match}");

        var items = new List<uint>();
        foreach (var offset in match)
        {
            items.Add(offset);
            Console.WriteLine($"nth  : {offset}");
        }

        Assert.Multiple(() =>
        {
            Assert.That(items.Count, Is.EqualTo(2));
            Assert.That(items[0], Is.EqualTo(4));
            Assert.That(items[1], Is.EqualTo(5));
        });
    }

    [Test]
    public unsafe void TestMatchEmptyOrDelete1()
    {
        var group = 0b0000_0000__0000_0001__0000_0000__1111_1111__0000_0000__0111_1110__1000_0000__0000_0000UL;
        var match = MatchEmptyOrDelete(*(Vector64<byte>*)&group);

        Console.WriteLine($"group: {new MatchBits(group)}");
        Console.WriteLine($"match: {match}");

        var items = new List<uint>();
        foreach (var offset in match)
        {
            items.Add(offset);
            Console.WriteLine($"nth  : {offset}");
        }

        Assert.Multiple(() =>
        {
            Assert.That(items.Count, Is.EqualTo(2));
            Assert.That(items[0], Is.EqualTo(1));
            Assert.That(items[1], Is.EqualTo(4));
        });
    }

    [Test]
    public unsafe void TestMatchValue1()
    {
        var group = 0b0000_0000__0000_0001__0000_0000__1111_1111__0000_0000__0111_1110__1000_0000__1000_0000UL;
        var match = MatchValue(*(Vector64<byte>*)&group);

        Console.WriteLine($"group: {new MatchBits(group)}");
        Console.WriteLine($"match: {match}");

        var items = new List<uint>();
        foreach (var offset in match)
        {
            items.Add(offset);
            Console.WriteLine($"nth  : {offset}");
        }

        Assert.Multiple(() =>
        {
            Assert.That(items.Count, Is.EqualTo(5));
            Assert.That(items[0], Is.EqualTo(2));
            Assert.That(items[1], Is.EqualTo(3));
            Assert.That(items[2], Is.EqualTo(5));
            Assert.That(items[3], Is.EqualTo(6));
            Assert.That(items[4], Is.EqualTo(7));
        });
    }
}

#endif
