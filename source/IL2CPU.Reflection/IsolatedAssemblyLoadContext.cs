using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cosmos.IL2CPU;

namespace IL2CPU.Reflection
{
    public class IsolatedAssemblyLoadContext : PathAssemblyResolver
    {
        public static IsolatedAssemblyLoadContext Default;

        private readonly MetadataLoadContext _context;
        private readonly Dictionary<AssemblyIdentity, Lazy<Assembly>> _assemblies;

        public IsolatedAssemblyLoadContext(IEnumerable<string> assemblyPaths,
            string netCoreName = "System.Private.CoreLib") : base(assemblyPaths)
        {
            _assemblies = new Dictionary<AssemblyIdentity, Lazy<Assembly>>();
            _context = new MetadataLoadContext(this, coreAssemblyName: netCoreName);

            var paths = assemblyPaths.OrderBy(p => p).Distinct();
            foreach (var assemblyPath in paths)
            {
                LoadOrAddByPath(assemblyPath);
            }
        }

        public void LoadOrAddByPath(string assemblyPath)
        {
            AssemblyName assemblyName;

            try
            {
                var name = Path.GetFileNameWithoutExtension(assemblyPath);
                assemblyName = new AssemblyName(name);
            }
            catch (ArgumentException e)
            {
                throw new FileLoadException($"Failed to get assembly name for '{assemblyPath}' !", e);
            }

            var assemblyIdentity = new AssemblyIdentity(assemblyName);

            if (_assemblies.ContainsKey(assemblyIdentity))
            {
                return;
            }

            _assemblies.Add(assemblyIdentity, new Lazy<Assembly>(
                () =>
                {
                    var asmbl = LoadFromAssemblyPath(assemblyPath);
                    return asmbl;
                }));
        }

        public Assembly LoadFromAssemblyPath(string path)
        {
            return _context.LoadFromAssemblyPath(path);
        }

        public Assembly LoadFromAssemblyName(AssemblyName name)
        {
            return _context.LoadFromAssemblyName(name);
        }

        public MetadataLoadContext GetLoader() => _context;

        public override Assembly Resolve(MetadataLoadContext context, AssemblyName assemblyName)
        {
            var asmbl = base.Resolve(context, assemblyName);
            if (asmbl == null)
            {
                var assemblyIdentity = new AssemblyIdentity(assemblyName);
                if (_assemblies.ContainsKey(assemblyIdentity))
                {
                    asmbl = _assemblies[assemblyIdentity].Value;
                }
                else
                {
                    var path = Assembly.Load(assemblyName).Location;
                    LoadOrAddByPath(path);
                    asmbl = _assemblies[assemblyIdentity].Value;
                }
            }
            return asmbl;
        }
    }
}
