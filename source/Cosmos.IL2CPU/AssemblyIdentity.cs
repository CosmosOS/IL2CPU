using System;
using System.Reflection;

namespace Cosmos.IL2CPU
{
    internal class AssemblyIdentity : IEquatable<AssemblyIdentity>
    {
        private AssemblyName _assemblyName;

        public AssemblyIdentity(AssemblyName assemblyName)
        {
            _assemblyName = assemblyName;
        }

        public bool Equals(AssemblyIdentity other) => _assemblyName.FullName == other._assemblyName.FullName;
        public override bool Equals(object obj) => obj is AssemblyIdentity other && Equals(other);
        public override int GetHashCode() => _assemblyName.FullName.GetHashCode();
        public override string ToString() => _assemblyName.FullName;
    }
}
