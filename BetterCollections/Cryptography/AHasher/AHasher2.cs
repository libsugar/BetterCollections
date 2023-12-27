using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BetterCollections.Cryptography.AHasherImpl;

#if NET7_0_OR_GREATER
using System.Runtime.Intrinsics;
using X86 = System.Runtime.Intrinsics.X86;
using Arm = System.Runtime.Intrinsics.Arm;
#endif

namespace BetterCollections.Cryptography;

// reference https://github.com/tkaitchuck/aHash/tree/master

/// <summary>
/// A hasher that ensures even distribution of each bit
/// <para>If possible use Aes SIMD acceleration (.net7+)</para>
/// </summary>
public partial struct AHasher2 : IHasher2
{
    #region GlobalRandomState

    public static AHasherRandomState GlobalRandomState
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
        get;
    } = GenerateRandomState();

    [ThreadStatic]
    private static AHasherRandomState _threadCurrentRandomState;
    [ThreadStatic]
    private static bool _threadCurrentHas;

    public static AHasherRandomState ThreadCurrentRandomState
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
        get
        {
            if (!_threadCurrentHas)
            {
                _threadCurrentRandomState = GenerateRandomState();
                _threadCurrentHas = true;
            }
            return _threadCurrentRandomState;
        }
    }

    #endregion

    #region RandomState

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasherRandomState GenerateRandomState()
    {
#if NETSTANDARD
        var rand = new Random();
#else
        var rand = Random.Shared;
#endif
        Unsafe.SkipInit(out AHasherRandomState state);
        rand.NextBytes(MemoryMarshal.Cast<ulong, byte>(state.AsSpan()));
        return state;
    }

    #endregion

    #region Create

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasher2 CreateGlobal()  => new(GlobalRandomState);

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasher2 CreateThreadCurrent() => new(ThreadCurrentRandomState);

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasher2 CreateRandom() => new(GenerateRandomState());

    #endregion

    #region Impl

#if NET7_0_OR_GREATER
    private Union union;

    [StructLayout(LayoutKind.Explicit)]
    private struct Union
    {
        [FieldOffset(0)]
        public AesHasher aesHasher;
        [FieldOffset(0)]
        public SoftHasher softHasher;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public AHasher2(AHasherRandomState randomState)
    {
        Unsafe.SkipInit(out this);
        if (AesHasher.IsSupported)
        {
            union.aesHasher = new AesHasher(randomState);
        }
        else
        {
            union.softHasher = new SoftHasher(randomState);
        }
    }

    #region IHasher

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void Add(int value)
    {
        if (AesHasher.IsSupported) union.aesHasher.Add(value);
        else union.softHasher.Add(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void Add(long value)
    {
        if (AesHasher.IsSupported) union.aesHasher.Add(value);
        else union.softHasher.Add(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void Add<T>(T value)
    {
        if (AesHasher.IsSupported) union.aesHasher.Add(value);
        else union.softHasher.Add(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void Add<T>(T value, IEqualityComparer<T>? comparer)
    {
        if (AesHasher.IsSupported) union.aesHasher.Add(value, comparer);
        else union.softHasher.Add(value, comparer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void AddBytes(ReadOnlySpan<byte> value)
    {
        if (AesHasher.IsSupported) union.aesHasher.AddBytes(value);
        else union.softHasher.AddBytes(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void AddString(ReadOnlySpan<byte> value)
    {
        if (AesHasher.IsSupported) union.aesHasher.AddString(value);
        else union.softHasher.AddString(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void AddString(ReadOnlySpan<char> value)
    {
        if (AesHasher.IsSupported) union.aesHasher.AddString(value);
        else union.softHasher.AddString(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public int ToHashCode() => AesHasher.IsSupported ? union.aesHasher.ToHashCode() : union.softHasher.ToHashCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public long ToHashCodeLong() =>
        AesHasher.IsSupported ? union.aesHasher.ToHashCodeLong() : union.softHasher.ToHashCodeLong();

    #endregion

#else
    private SoftHasher softHasher;

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public AHasher2(SoftHasher softHasher)
    {
        Unsafe.SkipInit(out this);
        this.softHasher = softHasher;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public AHasher2(AHasherRandomState randomState)
    {
        Unsafe.SkipInit(out this);
        softHasher = new SoftHasher(randomState);
    }

    #region IHasher

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void Add(int value) => softHasher.Add(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void Add(long value) => softHasher.Add(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void Add<T>(T value) => softHasher.Add(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void Add<T>(T value, IEqualityComparer<T>? comparer) => softHasher.Add(value, comparer);

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void AddBytes(ReadOnlySpan<byte> value) => softHasher.AddBytes(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void AddString(ReadOnlySpan<byte> value) => softHasher.AddString(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void AddString(ReadOnlySpan<char> value) => softHasher.AddString(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public int ToHashCode() => softHasher.ToHashCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public long ToHashCodeLong() => softHasher.ToHashCodeLong();

    #endregion

#endif

    #endregion

    #region Combine

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static int Combine<T1>(T1 value1)
    {
        var hasher = CreateGlobal();
        hasher.Add(value1);
        return hasher.ToHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static int Combine<T1, T2>(T1 value1, T2 value2)
    {
        var hasher = CreateGlobal();
        hasher.Add(value1);
        hasher.Add(value2);
        return hasher.ToHashCode();
    }

    #endregion
}
