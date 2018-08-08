using System;
using System.Linq;

using IL2CPU.Reflection;

namespace Cosmos.IL2CPU
{
    public static class GCImplementationRefs
    {
        public static readonly MethodInfo AllocNewObjectRef;
        public static readonly MethodInfo IncRefCountRef;
        public static readonly MethodInfo DecRefCountRef;

        static GCImplementationRefs()
        {
            var metadataContext = CompilerEngine.MetadataContext;

            TypeInfo xType = null;
            if (CompilerEngine.UseGen3Kernel)
            {
                xType = metadataContext.ResolveTypeByName("Cosmos.CPU.x86.GCImplementation, Cosmos.CPU.x86", true);
            }
            else
            {
                xType = metadataContext.ResolveTypeByName("Cosmos.Core.GCImplementation, Cosmos.Core", true);
            }
            if (xType == null)
            {
                throw new Exception("GCImplementation type not found!");
            }

            foreach (var xField in typeof(GCImplementationRefs).GetFields())
            {
                if (xField.Name.EndsWith("Ref"))
                {
                    var xTempMethod = xType.Methods.SingleOrDefault(
                        m => m.Name == xField.Name.Substring(0, xField.Name.Length - "Ref".Length));

                    if (xTempMethod == null)
                    {
                        throw new Exception("Method '" + xField.Name.Substring(0, xField.Name.Length - "Ref".Length) + "' not found on RuntimeEngine!");
                    }

                    xField.SetValue(null, xTempMethod);
                }
            }
        }
    }
}
