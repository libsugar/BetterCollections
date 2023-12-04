using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using BetterCollections;
using Microsoft.VisualBasic.CompilerServices;

namespace Tests;

public class TestMapHash
{
#if NET8_0_OR_GREATER
    [Test]
    public void ViewSimdJit_1()
    {
        var a = Vector128.Create((byte)0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0);
        var b = Vector128.Create((byte)1);

        for (int i = 0; i < 1000; i++)
        {
            if (i == 999)
            {
                Console.Write(999);
            }
            var z = DoSwissH2_1(a, b);
            Console.Write(z);
            Assert.That(z, Is.EqualTo(2));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
    private uint DoSwissH2_1(Vector128<byte> a, Vector128<byte> b)
    {
        var c = Vector128.Equals(a, b);
        var v = c.ExtractMostSignificantBits();
        var z = uint.TrailingZeroCount(v);
        return z;
    }

    [Test]
    public void ViewSimdJit_2()
    {
        var a = Vector512.Create((byte)0).WithElement(8, (byte)1);
        var b = Vector512.Create((byte)1);

        for (int i = 0; i < 1000; i++)
        {
            if (i == 999)
            {
                Console.Write(999);
            }
            var z = DoSwissH2_2(a, b);
            Console.Write(z);
            Assert.That(z, Is.EqualTo(8));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
    private uint DoSwissH2_2(Vector512<byte> a, Vector512<byte> b)
    {
        var c = Vector512.Equals(a, b);
        var v = c.ExtractMostSignificantBits();
        var z = (uint)ulong.TrailingZeroCount(v);
        return z;
    }

#endif
}
