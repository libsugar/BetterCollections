using System.Runtime.Intrinsics;
using BetterCollections.Misc;

namespace Tests;

public class TestSwissTableSoftImpl : ASwissTable
{
    [Test]
    public void TestSub1()
    {
        var a = 100U;
        var b = 100U;
        var c = uint.MaxValue;

        var x = unchecked(a - b);
        var y = unchecked(a - c);
        
        Console.WriteLine(x);
        Console.WriteLine(y);
        
        Assert.That(x, Is.EqualTo(0));
        Assert.That(y, Is.EqualTo(101));
    }
    
    [Test]
    public void TestMatchH2_1()
    {
        var group = 0b0000_0001__1111_1111__0000_0001__0000_0000__0111_1110__0000_0000__1000_0000__0000_0000UL;
        var h2 = Utils.CreateULong(0b0000_0001);
        var match = MatchH2(group, h2);
        
        Console.WriteLine($"h2   : {new MatchBits(h2)}");
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
    public void TestMatchEmpty1()
    {
        var group = 0b0000_0000__0000_0001__1111_1111__1111_1111__0000_0000__0111_1110__1000_0000__0000_0000UL;
        var match = MatchEmpty(group);
        
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
    public void TestMatchEmptyOrDelete1()
    {
        var group = 0b0000_0000__0000_0001__0000_0000__1111_1111__0000_0000__0111_1110__1000_0000__0000_0000UL;
        var match = MatchEmptyOrDelete(group);
        
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
    public void TestMatchValue1()
    {
        var group = 0b0000_0000__0000_0001__0000_0000__1111_1111__0000_0000__0111_1110__1000_0000__1000_0000UL;
        var match = MatchValue(group);
        
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
