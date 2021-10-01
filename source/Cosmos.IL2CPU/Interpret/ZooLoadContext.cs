using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using IL2CPU.Reflection;

namespace Cosmos.IL2CPU.Interpret
{
    internal class ZooLoadContext : AssemblyLoadContext
    {
        private readonly IDictionary<string, Assembly> _assemblies;

        public ZooLoadContext()
        {
            _assemblies = new SortedDictionary<string, Assembly>();
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            var name = assemblyName.ToString()
                .Split(new[] { ", Version=" }, StringSplitOptions.None)
                .First();
            if (_assemblies.TryGetValue(name, out var found))
            {
                return found;
            }
            var newName = new AssemblyName(name);
            var dll = TryLoad(newName);
            if (dll != null)
            {
                _assemblies[name] = dll;
            }
            return dll;
        }

        private Assembly TryLoad(AssemblyName name)
        {
            try
            {
                var dll = Assembly.Load(name);
                return dll;
            }
            catch (Exception)
            {
                var refl = IsolatedAssemblyLoadContext.Default;
                var reflDll = refl.LoadFromAssemblyName(name);
                var maybe = LoadFromAssemblyPath(reflDll.Location);
                return maybe;
            }
        }
    }
}
