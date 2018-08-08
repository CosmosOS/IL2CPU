using System;
using System.Linq;
using System.Reflection;

namespace IL2CPU.Reflection
{
    public static class MetadataContextExtensions
    {
        public static TypeInfo ImportType<T>(this MetadataContext metadataContext) =>
            metadataContext.ImportType(typeof(T));

        public static TypeInfo ImportType(
            this MetadataContext metadataContext,
            Type type)
        {
            var module = metadataContext.ImportModule(type.Module);

            if (type.DeclaringType != null)
            {
                var declaringType = type.DeclaringType;
                return metadataContext.ImportType(declaringType).NestedTypes.SingleOrDefault(
                    t => t.Name == type.Name);
            }

            return module.Types.SingleOrDefault(
                t => t.Namespace == type.Namespace && t.Name == type.Name);
        }

        public static ModuleInfo ImportModule(
            this MetadataContext metadataContext,
            Module module)
        {
            var assembly = metadataContext.ResolveAssembly(
                AssemblyIdentity.FromAssemblyName(module.Assembly.GetName()));
            return assembly?.Modules.SingleOrDefault(m => m.Name == module.Name);
        }
    }
}
