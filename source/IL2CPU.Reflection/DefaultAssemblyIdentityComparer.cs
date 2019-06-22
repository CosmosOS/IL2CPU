using System;
using System.Collections.Generic;

namespace IL2CPU.Reflection
{
    internal class DefaultAssemblyIdentityComparer : IEqualityComparer<AssemblyIdentity>
    {
        public int GetHashCode(AssemblyIdentity obj) => obj.Name.GetHashCode();
        public bool Equals(AssemblyIdentity x, AssemblyIdentity y) =>
            String.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
    }
}