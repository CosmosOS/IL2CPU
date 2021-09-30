using System;
using System.Collections.Generic;

namespace IL2CPU.Reflection
{
    public static class StandardExtensions
    {
        public static bool TryGetValue<T>(this ISet<T> set, T key, out T value)
        {
            foreach (var item in set)
            {
                if (item.ToString() == key.ToString())
                {
                    value = item;
                    return true;
                }
            }
            value = default;
            return false;
        }

        public static bool IsAssignableTo(this Type target, Type source)
        {
            return source.IsAssignableFrom(target);
        }
    }
}
