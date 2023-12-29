﻿#if NETSTANDARD || NET6_0
namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Parameter, Inherited = false)]
internal sealed class UnscopedRefAttribute : Attribute;

#endif
