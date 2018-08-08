using System;
using System.Linq;

using IL2CPU.Reflection;

namespace Cosmos.IL2CPU
{
    public static class RuntimeEngineRefs
    {
        public static readonly MethodInfo FinalizeApplicationRef;
        public static readonly MethodInfo InitializeApplicationRef;
        public static readonly MethodInfo Heap_AllocNewObjectRef;

        static RuntimeEngineRefs()
        {
            var xType = CompilerEngine.MetadataContext.ImportType(typeof(RuntimeEngine));

            foreach (var xField in typeof(RuntimeEngineRefs).GetFields())
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
