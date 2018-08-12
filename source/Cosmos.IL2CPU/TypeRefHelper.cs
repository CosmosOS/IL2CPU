using System;

using IL2CPU.Reflection;

namespace Cosmos.IL2CPU
{
    public static class TypeRefHelper
    {
        public static TypeInfo GetBclType(BclType type) => CompilerEngine.MetadataContext.GetBclType(type);

        public static TypeInfo TypeOf(BclType type) => GetBclType(type);

        public static TypeInfo TypeOf<T>() => CompilerEngine.MetadataContext.ImportType<T>();
        public static TypeInfo TypeOf(Type type) => CompilerEngine.MetadataContext.ImportType(type);
    }
}
