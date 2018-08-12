using System;

namespace IL2CPU.Reflection
{
    public static class TypeExtensions
    {
        public static TypeInfo Import(
            this Type type,
            MetadataContext metadataContext) => metadataContext.ImportType(type);
    }
}
