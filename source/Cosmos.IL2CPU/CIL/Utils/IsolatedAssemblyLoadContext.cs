using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Cosmos.IL2CPU
{
    internal class IsolatedAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly Dictionary<AssemblyIdentity, Lazy<Assembly>> _assemblies;

        public IsolatedAssemblyLoadContext(IEnumerable<string> assemblyPaths)
        {
            _assemblies = new Dictionary<AssemblyIdentity, Lazy<Assembly>>();

            foreach (var assemblyPath in assemblyPaths)
            {
                AssemblyName assemblyName;

                try
                {
                    assemblyName = GetAssemblyName(assemblyPath);
                }
                catch (ArgumentException e)
                {
                    throw new FileLoadException($"Failed to get assembly name for '{assemblyPath}' !", e);
                }

                var assemblyIdentity = new AssemblyIdentity(assemblyName);

                if (_assemblies.ContainsKey(assemblyIdentity))
                {
                    continue;
                }
                else
                {
                    // HACK: need to fix assembly loading
                    if (!AppDomain.CurrentDomain.GetAssemblies().Any(
                        a => new AssemblyIdentity(a.GetName()).Equals(assemblyIdentity)))
                    {
                        Default.LoadFromAssemblyPath(assemblyPath);
                    }

                    _assemblies.Add(
                        assemblyIdentity,
                        new Lazy<Assembly>(
                            () =>
                            {
                                // HACK: need to fix assembly loading
                                return Default.LoadFromAssemblyPath(assemblyPath);

                                //return LoadFromAssemblyPath(assemblyPath);
                            }));
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
