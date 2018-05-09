using System;
using System.Collections.Generic;
using System.Reflection;

using Cosmos.IL2CPU.Extensions;

namespace Cosmos.IL2CPU
{
    public class MethodBaseComparer : IComparer<MethodBase>, IEqualityComparer<MethodBase>
    {
        public int Compare(MethodBase x, MethodBase y) =>
            String.Compare(x.GetFullName(), y.GetFullName(), StringComparison.Ordinal);

        public bool Equals(MethodBase x, MethodBase y) =>
            String.Equals(x.GetFullName(), y.GetFullName(), StringComparison.Ordinal);

        public int GetHashCode(MethodBase obj) => obj.GetFullName().GetHashCode();
    }
}
