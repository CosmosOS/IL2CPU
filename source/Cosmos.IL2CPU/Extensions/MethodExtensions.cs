using System.Collections.Generic;

using IL2CPU.API;
using IL2CPU.Reflection;

namespace Cosmos.IL2CPU.Extensions
{
    internal static class MethodExtensions
    {
        public static string GetFullName(this MethodInfo aMethod)
        {
            return LabelName.Get(aMethod);
        }
    }
}
