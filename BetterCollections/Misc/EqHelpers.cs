// using System.Collections.Generic;
// using System.Runtime.CompilerServices;
//
// namespace BetterCollections.Misc;
//
// internal static class EqHelpers
// {
//     [MethodImpl(MethodImplOptions.AggressiveInlining)]
//     public static IEqualityComparer<T>? NormalizeEqualityComparer<T>(IEqualityComparer<T>? comparer)
//     {
//         if (typeof(T).IsValueType)
//         {
//             if (comparer is not null && !ReferenceEquals(comparer, EqualityComparer<T>.Default)) return comparer;
//             return null;
//         }
//         else
//         {
//             return comparer ?? EqualityComparer<T>.Default;
//         }
//     }
//
//     [MethodImpl(MethodImplOptions.AggressiveInlining)]
//     public static int GetHashCode<T>(T value, IEqualityComparer<T>? comparer)
//     {
//         if (typeof(T).IsValueType && comparer is null) return value!.GetHashCode();
//         return value == null ? 0 : comparer!.GetHashCode(value);
//     }
// }
