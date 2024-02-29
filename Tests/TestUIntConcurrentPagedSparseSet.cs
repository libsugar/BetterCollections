using BetterCollections.Concurrent;

namespace Tests;

public class TestUIntConcurrentPagedSparseSet
{
    [Test]
    public void Test1()
    {
        var set = new UIntConcurrentPagedSparseSet();
        set.Add(3);
        Assert.That(set.Contains(3), Is.True);
    }

    // [Test]
    // public void Test2()
    // {
    //     var set = new UIntConcurrentPagedSparseSet();
    //     set.Add(3);
    //     set.Remove(3);
    //     Assert.That(set.Contains(3), Is.False);
    // }
    //
    // [Test]
    // public void Test3()
    // {
    //     var set = new UIntConcurrentPagedSparseSet();
    //     set.Add(3);
    //     set.Add(5);
    //     set.Remove(3);
    //     Assert.That(set.Contains(3), Is.False);
    //     Assert.That(set.Contains(5), Is.True);
    // }
    //
    // [Test]
    // public void Test4()
    // {
    //     var set = new UIntConcurrentPagedSparseSet();
    //     set.Add(3);
    //     set.Add(5);
    //     set.Add(10);
    //     set.Remove(3);
    //     set.Remove(10);
    //     Assert.That(set.Contains(3), Is.False);
    //     Assert.That(set.Contains(5), Is.True);
    //     Assert.That(set.Contains(10), Is.False);
    // }
    //
    // [Test]
    // public void Test5()
    // {
    //     var set = new UIntConcurrentPagedSparseSet();
    //     set.Add(3);
    //     set.Add(5);
    //     set.Remove(3);
    //     set.Remove(5);
    //     Assert.That(set.Contains(3), Is.False);
    //     Assert.That(set.Contains(5), Is.False);
    // }

    [Test]
    public void Test6()
    {
        var set = new UIntConcurrentPagedSparseSet();
        ParallelEnumerable.Range(0, 10000).ForAll(i => { set.Add((uint)i); });
        Assert.That(set.Count, Is.EqualTo(10000));
        ParallelEnumerable.Range(0, 10000).ForAll(i =>
        {
            Assert.That((set.Contains((uint)i), i), Is.EqualTo((true, i)));
        });
    }

#if NET8_0_OR_GREATER

    [Test]
    public void Test6_rand()
    {
        var set = new UIntConcurrentPagedSparseSet();
        var except = ParallelEnumerable.Range(0, 10000).Select(a => (uint)a).ToArray();
        Random.Shared.Shuffle(except);
        ParallelEnumerable.Range(0, 10000).ForAll(i => { set.Add(except[i]); });
        Assert.That(set.Count, Is.EqualTo(10000));
        ParallelEnumerable.Range(0, 10000).ForAll(i =>
        {
            Assert.That((set.Contains((uint)i), i), Is.EqualTo((true, i)));
        });
    }

#endif

    [Test]
    public void Test7()
    {
        var set = new UIntConcurrentPagedSparseSet();
        ParallelEnumerable.Range(0, 10000).ForAll(i => { set.Add((uint)i); });
        var arr = set.Select(a => a).OrderBy(a => a).ToArray();
        var except = ParallelEnumerable.Range(0, 10000).Select(a => (uint)a).ToArray();
        Assert.That(arr, Is.EqualTo(except));
    }
}
