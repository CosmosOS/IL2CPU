using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;

namespace Cosmos.IL2CPU
{
    internal class IsolatedAssemblyLoadContext : AssemblyLoadContext
    {
        private Dictionary<AssemblyIdentity, Lazy<Assembly>> _assemblies;

        public IsolatedAssemblyLoadContext(IEnumerable<string> assemblyPaths)
        {
            _assemblies = new Dictionary<AssemblyIdentity, Lazy<Assembly>>();

            foreach (var assemblyPath in assemblyPaths)
            {
                var assemblyIdentity = new AssemblyIdentity(GetAssemblyName(assemblyPath));

                if (_assemblies.ContainsKey(assemblyIdentity))
                {
                    throw new NotSupportedException("Cannot load more than 1 assembly with the same identity!");
                }
                else
                {
                    _assemblies.Add(
                        assemblyIdentity,
                        new Lazy<Assembly>(() => LoadFromAssemblyPath(assemblyPath)));
                }
            }
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            var assemblyIdentity = new AssemblyIdentity(assemblyName);
            _assemblies.TryGetValue(assemblyIdentity, out var assembly);

            return assembly?.Value;
        }
    }
}
