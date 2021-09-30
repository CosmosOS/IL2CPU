using System;
using System.Collections.Generic;

namespace IL2CPU.Reflection
{
    public static class StandardExtensions
    {
        public static bool TryGetMyValue<T>(this ISet<T> set, T equal, out T actual, IEqualityComparer<T> cmp = null)
        {
            var equalHash = equal != null ? (cmp?.GetHashCode(equal) ?? equal.GetHashCode()) : 0;
            foreach (var item in set)
            {
                var itemHash = item != null ? (cmp?.GetHashCode(item) ?? item.GetHashCode()) : 0;
                if (equalHash == itemHash)
                {
                    actual = item;
                    return true;
                }
            }
            actual = default;
            return false;
        }

        public static bool IsMyAssignableTo(this Type @this, Type targetType)
        {
            return targetType != null && targetType.IsAssignableFrom(@this);
        }
    }
}
