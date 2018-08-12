using System;
using System.Reflection.Metadata;

namespace IL2CPU.Reflection.Internal
{
    internal static class ConstantExtensions
    {
        public static object GetConstantValue(this Constant constant, ModuleInfo module)
        {
            var valueReader = module.MetadataReader.GetBlobReader(constant.Value);
            return valueReader.ReadConstant(constant.TypeCode);
        }
    }
}
