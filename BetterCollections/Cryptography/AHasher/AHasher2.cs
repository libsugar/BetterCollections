#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BetterCollections.Cryptography.AHasherImpl;
using System.Runtime.Intrinsics;
using X86 = System.Runtime.Intrinsics.X86;
using Arm = System.Runtime.Intrinsics.Arm;


namespace BetterCollections.Cryptography;

// reference https://github.com/tkaitchuck/aHash/tree/master

/// <summary>
/// A hasher that ensures even distribution of each bit
/// <para>If possible use Aes SIMD acceleration (.net7+)</para>
/// </summary>
public struct AHasher2 : IHasher2
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
        var rand = Random.Shared;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public AHasher2(AHasherRandomState randomState)
    {
        Unsafe.SkipInit(out this);
        data = AesHasher.IsSupported ? AesHasher.Create(randomState) : SoftHasher.Create(randomState);
    }

    #region IHasher

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void Add(int value)
    {
        data = AesHasher.IsSupported ? AesHasher.Add(data, value) : SoftHasher.Add(data, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void Add(long value)
    {
        data = AesHasher.IsSupported ? AesHasher.Add(data, value) : SoftHasher.Add(data, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void Add<T>(T value)
    {
        data = AesHasher.IsSupported ? AesHasher.Add(data, value) : SoftHasher.Add(data, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void Add<T>(T value, IEqualityComparer<T>? comparer)
    {
        data = AesHasher.IsSupported ? AesHasher.Add(data, value, comparer) : SoftHasher.Add(data, value, comparer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void AddBytes(ReadOnlySpan<byte> value)
    {
        data = AesHasher.IsSupported ? AesHasher.AddBytes(data, value) : SoftHasher.AddBytes(data, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void AddString(ReadOnlySpan<byte> value)
    {
        data = AesHasher.IsSupported ? AesHasher.AddString(data, value) : SoftHasher.AddString(data, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public void AddString(ReadOnlySpan<char> value)
    {
        data = AesHasher.IsSupported ? AesHasher.AddString(data, value) : SoftHasher.AddString(data, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public int ToHashCode() =>
        AesHasher.IsSupported ? AesHasher.ToHashCode(data) : SoftHasher.ToHashCode(data);

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public long ToHashCodeLong() =>
        AesHasher.IsSupported ? AesHasher.ToHashCodeLong(data) : SoftHasher.ToHashCodeLong(data);

    #endregion

    #endregion

    #region Combine

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static int Combine<T1>(T1 value1)
    {
        if (AesHasher.IsSupported)
        {
            return AesHasher.Combine(value1);
        }
        throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), SkipLocalsInit]
    public static int Combine<T1, T2>(T1 value1, T2 value2)
    {
        if (AesHasher.IsSupported)
        {
            return AesHasher.Combine(value1, value2);
        }
        throw new NotImplementedException();
    }

    #endregion
}

#endif
