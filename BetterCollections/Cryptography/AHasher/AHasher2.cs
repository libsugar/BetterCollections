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
        get => _globalRandomState;
    }

    public static readonly AHasherRandomState _globalRandomState = GenerateRandomState();

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
        rand.NextBytes(MemoryMarshal.Cast<ulong, byte>(state.UnsafeAsMutableSpan()));
        return state;
    }

    #endregion

    #region Create

    public static AHasher2 Global
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
        get;
    } = new(GlobalRandomState);

    public static AHasher2 ThreadCurrent
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
        get;
    } = new(ThreadCurrentRandomState);

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static AHasher2 CreateRandom() => new(GenerateRandomState());
    
    #endregion

    #region Impl

    private AHasher2Data data;

#if NET7_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public AHasher2(AHasherRandomState randomState)
    {
        Unsafe.SkipInit(out this);
        if (AesHasher.IsSupported)
        {
            data = AesHasher.Create(randomState);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    #region IHasher

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void Add(int value)
    {
        if (AesHasher.IsSupported) AesHasher.Add(ref data, value);
        else throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void Add(long value)
    {
        if (AesHasher.IsSupported) AesHasher.Add(ref data, value);
        else throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void Add<T>(T value)
    {
        if (AesHasher.IsSupported) AesHasher.Add(ref data, value);
        else throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void Add<T>(T value, IEqualityComparer<T>? comparer)
    {
        if (AesHasher.IsSupported) AesHasher.Add(ref data, value, comparer);
        else throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void AddBytes(ReadOnlySpan<byte> value)
    {
        if (AesHasher.IsSupported) AesHasher.AddBytes(ref data, value);
        else throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void AddString(ReadOnlySpan<byte> value)
    {
        if (AesHasher.IsSupported) AesHasher.AddString(ref data, value);
        else throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void AddString(ReadOnlySpan<char> value)
    {
        if (AesHasher.IsSupported) AesHasher.AddString(ref data, value);
        else throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public int ToHashCode() =>
        AesHasher.IsSupported ? AesHasher.ToHashCode(ref data) : throw new NotImplementedException();

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public long ToHashCodeLong() =>
        AesHasher.IsSupported ? AesHasher.ToHashCodeLong(ref data) : throw new NotImplementedException();

    #endregion

#else
    private SoftHasher softHasher;

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public AHasher2(AHasherRandomState randomState)
    {
        Unsafe.SkipInit(out this);
        throw new NotImplementedException();
    }

    #region IHasher

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void Add(int value) => throw new NotImplementedException();

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void Add(long value) => throw new NotImplementedException();

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void Add<T>(T value) => throw new NotImplementedException();

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void Add<T>(T value, IEqualityComparer<T>? comparer) => throw new NotImplementedException();

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void AddBytes(ReadOnlySpan<byte> value) => throw new NotImplementedException();

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void AddString(ReadOnlySpan<byte> value) => throw new NotImplementedException();

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void AddString(ReadOnlySpan<char> value) => throw new NotImplementedException();

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public int ToHashCode() => throw new NotImplementedException();

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public long ToHashCodeLong() => throw new NotImplementedException();

    #endregion

#endif

    #endregion

    #region Combine

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static int Combine<T1>(T1 value1)
    {
#if NET7_0_OR_GREATER
        if (AesHasher.IsSupported)
        {
            return AesHasher.Combine(value1);
        }
#endif
        throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static int Combine<T1, T2>(T1 value1, T2 value2)
    {
#if NET7_0_OR_GREATER
        if (AesHasher.IsSupported)
        {
            return AesHasher.Combine(value1, value2);
        }
#endif
        throw new NotImplementedException();
    }

    #endregion
}
