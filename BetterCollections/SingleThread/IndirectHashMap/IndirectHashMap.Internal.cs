using System;
using System.Buffers;
using System.Runtime.InteropServices;
#if NET7_0_OR_GREATER
using System.Runtime.Intrinsics;
#endif

namespace BetterCollections.IndirectHashMap_Internal;

internal enum GroupType
{
    ULong,
#if NET7_0_OR_GREATER
        Vector64,
        Vector128,
        Vector256,
#if NET8_0_OR_GREATER
        Vector512,
#endif
#endif
}

internal struct Meta(uint HashCode, int Index)
{
    public uint HashCode = HashCode;
    public int Index = Index;

    public override string ToString() => $"Meta(hash: {HashCode}, index: {Index})";
}

[StructLayout(LayoutKind.Explicit)]
internal struct CtrlArrayPool(object ArrayPool)
{
    [FieldOffset(0)]
    public object ArrayPool = ArrayPool;
    [FieldOffset(0)]
    public ArrayPool<ulong> ULong;
#if NET7_0_OR_GREATER
    [FieldOffset(0)]
    public ArrayPool<Vector64<byte>> Vector64;
    [FieldOffset(0)]
    public ArrayPool<Vector128<byte>> Vector128;
    [FieldOffset(0)]
    public ArrayPool<Vector256<byte>> Vector256;
#if NET8_0_OR_GREATER
    [FieldOffset(0)]
    public ArrayPool<Vector512<byte>> Vector512;
#endif
#endif
}

[StructLayout(LayoutKind.Explicit)]
internal struct CtrlArray(Array Array)
{
    [FieldOffset(0)]
    public Array Array = Array;
    [FieldOffset(0)]
    public ulong[]? ULong;
#if NET7_0_OR_GREATER
    [FieldOffset(0)]
    public Vector64<byte>[]? Vector64;
    [FieldOffset(0)]
    public Vector128<byte>[]? Vector128;
    [FieldOffset(0)]
    public Vector256<byte>[]? Vector256;
#if NET8_0_OR_GREATER
    [FieldOffset(0)]
    public Vector512<byte>[]? Vector512;
#endif
#endif
}
