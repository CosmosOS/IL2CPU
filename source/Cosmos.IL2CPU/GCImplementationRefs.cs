using System;
using System.Reflection;

namespace Cosmos.IL2CPU
{
    public static class GCImplementationRefs
    {
        public static readonly MethodBase AllocNewObjectRef;
        public static readonly MethodBase InitRef;
        public static readonly MethodBase IncRootCountRef;
        public static readonly MethodBase DecRootCountRef;
        public static readonly MethodBase IncRootCountsInStructRef;
        public static readonly MethodBase DecRootCountsInStructRef;

        static GCImplementationRefs()
        {
            var typeResolver = CompilerEngine.TypeResolver;

            Type xType = null;
            xType = typeResolver.ResolveType("Cosmos.Core.GCImplementation, Cosmos.Core", true);
            if (xType == null)
            {
                throw new Exception("GCImplementation type not found!");
            }

            foreach (FieldInfo xField in typeof(GCImplementationRefs).GetFields())
            {
                if (xField.Name.EndsWith("Ref"))
                {
                    MethodBase xTempMethod = xType.GetMethod(xField.Name.Substring(0, xField.Name.Length - "Ref".Length));
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