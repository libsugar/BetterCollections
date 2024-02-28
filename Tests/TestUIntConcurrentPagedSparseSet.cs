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
    
    [Test]
    public void Test2()
    {
        var set = new UIntConcurrentPagedSparseSet();
        set.Add(3);
        set.Remove(3);
        Assert.That(set.Contains(3), Is.False);
    }
    
    [Test]
    public void Test3()
    {
        var set = new UIntConcurrentPagedSparseSet();
        set.Add(3);
        set.Add(5);
        set.Remove(3);
        Assert.That(set.Contains(3), Is.False);
        Assert.That(set.Contains(5), Is.True);
    }
    
    [Test]
    public void Test4()
    {
        var set = new UIntConcurrentPagedSparseSet();
        set.Add(3);
        set.Add(5);
        set.Add(10);
        set.Remove(3);
        set.Remove(10);
        Assert.That(set.Contains(3), Is.False);
        Assert.That(set.Contains(5), Is.True);
        Assert.That(set.Contains(10), Is.False);
    }
    
    [Test]
    public void Test5()
    {
        var set = new UIntConcurrentPagedSparseSet();
        set.Add(3);
        set.Add(5);
        set.Remove(3);
        set.Remove(5);
        Assert.That(set.Contains(3), Is.False);
        Assert.That(set.Contains(5), Is.False);
    }
    
    
    [Test]
    public void Test6()
    {
        var set = new UIntConcurrentPagedSparseSet();
        ParallelEnumerable.Range(0, 10000).ForAll(i =>
        {
            set.Add((uint)i);
        });
        Console.WriteLine(set);
    }
}
