using System.Runtime.CompilerServices;
using BetterCollections.Memories;

namespace Tests;

[Parallelizable]
public class TestOffsetRef
{
    class Class1
    {
        // ReSharper disable once NotAccessedField.Local
        public int a;
        public int b;
    }

    [Test]
    public void Test1()
    {
        var obj = new Class1 { a = 123, b = 456 };
        var r = OffsetRef.UnsafeCreate(obj, ref obj.b);
        Assert.That(r.Value, Is.EqualTo(456));
    }

    [Test]
    public void Test2()
    {
        var obj = new[] { 123, 456 };
        var r = OffsetRef.UnsafeCreate(obj, ref obj[1]);
        Assert.That(r.Value, Is.EqualTo(456));
    }

    [Test, NonParallelizable, Repeat(3)]
    public unsafe void Test3()
    {
        var obj = new Class1 { a = 123, b = 456 };
        var r = OffsetRef.UnsafeCreate(obj, ref obj.b);
        
        Console.WriteLine(*(nuint*)Unsafe.AsPointer(ref obj));
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        Console.WriteLine(*(nuint*)Unsafe.AsPointer(ref obj));
        
        Assert.That(r.Value, Is.EqualTo(456));
    }
}
