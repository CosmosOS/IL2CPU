using System;
using System.Reflection;
using System.Runtime.Loader;
using IL2CPU.Reflection;

namespace Cosmos.IL2CPU
{
    internal class TypeResolver
    {
        private IsolatedAssemblyLoadContext _assemblyLoadContext;

        public TypeResolver(IsolatedAssemblyLoadContext assemblyLoadContext)
        {
            _assemblyLoadContext = assemblyLoadContext ?? throw new ArgumentNullException(nameof(assemblyLoadContext));
        }

        public Type ResolveType(string typeName) => Type.GetType(typeName, ResolveAssembly, ResolveType);
        public Type ResolveType(string typeName, bool throwOnError) =>
            Type.GetType(typeName, ResolveAssembly, ResolveType, throwOnError);
        public Type ResolveType(string typeName, bool throwOnError, bool ignoreCase) =>
            Type.GetType(typeName, ResolveAssembly, ResolveType, throwOnError, ignoreCase);

        private Assembly ResolveAssembly(AssemblyName assemblyName) =>
            _assemblyLoadContext.LoadFromAssemblyName(assemblyName);
        private Type ResolveType(Assembly assembly, string typeName, bool ignoreCase) =>
            assembly.GetType(typeName, false, ignoreCase);
    }
}
