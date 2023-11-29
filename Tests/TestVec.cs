using System.Buffers;
using BetterCollections;

namespace Tests;

[Parallelizable]
public class TestVec
{
    [Test]
    public void TestAdd1()
    {
        var vec = new Vec<int> { 1, 2, 3 };
        Assert.That(vec.SequenceEqual(new[] { 1, 2, 3 }), Is.True);
    }

    [Test]
    public void TestAdd2()
    {
        var vec = new Vec<int> { 1, 2, 3, 4, 5, 6 };
        Assert.That(vec.SequenceEqual(new[] { 1, 2, 3, 4, 5, 6 }), Is.True);
    }

    [Test]
    public void TestAdd3()
    {
        var vec = new Vec<string> { "a", "s", "d" };
        Assert.That(vec.SequenceEqual(new[] { "a", "s", "d" }), Is.True);
    }

    [Test]
    public void TestAdd4()
    {
        var vec = new Vec<string> { "a", "s", "d", "q", "w", "e" };
        Assert.That(vec.SequenceEqual(new[] { "a", "s", "d", "q", "w", "e" }), Is.True);
    }

    [Test]
    public void TestAdd5()
    {
        var vec = new Vec<int>(2);
        vec.Add(1);
        vec.Add(2);
        vec.Add(3);
        Assert.That(vec.SequenceEqual(new[] { 1, 2, 3 }), Is.True);
    }

    [Test]
    public void TestClear1()
    {
        var vec = new Vec<int> { 1, 2, 3 };
        vec.Clear();
        Assert.That(vec.SequenceEqual(Array.Empty<int>()), Is.True);
    }

    [Test]
    public void TestClear2()
    {
        var vec = new Vec<string> { "a", "s", "d" };
        vec.Clear();
        Assert.That(vec.SequenceEqual(Array.Empty<string>()), Is.True);
    }

    [Test]
    public void TestInsert1()
    {
        var vec = new Vec<int> { 1, 3, 7, 9 };
        vec.Insert(2, 5);
        Assert.That(vec.SequenceEqual(new[] { 1, 3, 5, 7, 9 }), Is.True);
    }

    [Test]
    public void TestInsert2()
    {
        var vec = new Vec<int> { 1, 3, 7 };
        vec.Insert(2, 5);
        Assert.That(vec.SequenceEqual(new[] { 1, 3, 5, 7 }), Is.True);
    }

    [Test]
    public void TestInsert3()
    {
        var vec = new Vec<int> { 1, 3, 7, 9 };
        vec.Insert(4, 5);
        Assert.That(vec.SequenceEqual(new[] { 1, 3, 7, 9, 5 }), Is.True);
    }

    [Test]
    public void TestRemove1()
    {
        var vec = new Vec<int> { 1, 3, 5, 7, 9 };
        vec.Remove(5);
        Assert.That(vec.SequenceEqual(new[] { 1, 3, 7, 9 }), Is.True);
    }

    [Test]
    public void TestRemove2()
    {
        var vec = new Vec<string> { "a", "s", "d", "q", "w", "e" };
        vec.Remove("d");
        Assert.That(vec.SequenceEqual(new[] { "a", "s", "q", "w", "e" }), Is.True);
    }

    [Test]
    public void TestRemove3()
    {
        var vec = new Vec<int> { 1 };
        vec.RemoveAt(0);
        Assert.That(vec.SequenceEqual(Array.Empty<int>()), Is.True);
    }

    [Test]
    public void TestRemove4()
    {
        var vec = new Vec<int> { 1, 2, 3 };
        vec.RemoveAt(1);
        Assert.That(vec.SequenceEqual(new[] { 1, 3 }), Is.True);
    }

    [Test]
    public void TestRemove5()
    {
        var vec = new Vec<int> { 1, 2, 3 };
        vec.RemoveAt(2);
        Assert.That(vec.SequenceEqual(new[] { 1, 2 }), Is.True);
    }

    [Test]
    public void TestCopyTo1()
    {
        var vec = new Vec<int> { 1, 2, 3 };
        var arr = new int[3];
        vec.CopyTo(arr, 0);
        Assert.That(arr.SequenceEqual(new[] { 1, 2, 3 }), Is.True);
    }

    [Test]
    public void TestCopyTo2()
    {
        var vec = new Vec<int> { 1, 2, 3 };
        var arr = new int[4];
        vec.CopyTo(arr, 1);
        Assert.That(arr.SequenceEqual(new[] { 0, 1, 2, 3 }), Is.True);
    }

    [Test]
    public void TestPoolAdd1()
    {
        using var vec = new Vec<int>(ArrayPool<int>.Shared);
        for (int i = 0; i < 100; i++)
        {
            vec.Add(i);
        }
    }
}
