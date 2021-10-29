﻿using System;
using System.Reflection;

namespace Cosmos.IL2CPU
{
    /// <summary>
    /// Wrapper for <seealso cref="AssemblyName" />, used to compare <seealso cref="AssemblyName" /> objects equality.
    /// Only the assembly name is compared (<seealso cref="AssemblyName.Name" />).
    /// </summary>
    internal class AssemblyIdentity : IEquatable<AssemblyIdentity>
    {
        private AssemblyName _assemblyName;

        public AssemblyIdentity(AssemblyName assemblyName)
        {
            _assemblyName = assemblyName;
        }

        public bool Equals(AssemblyIdentity other) => _assemblyName.Name == other._assemblyName.Name;
        public override bool Equals(object obj) => obj is AssemblyIdentity other && Equals(other);
        public override int GetHashCode() => _assemblyName.Name.GetHashCode();
        public override string ToString() => _assemblyName.Name;
    }
}
