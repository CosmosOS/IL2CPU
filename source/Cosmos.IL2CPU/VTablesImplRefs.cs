using System;
using System.Linq;

using IL2CPU.Reflection;

namespace Cosmos.IL2CPU
{
    internal static class VTablesImplRefs
    {
        public static readonly MethodInfo SetTypeInfoRef;
        public static readonly MethodInfo SetInterfaceInfoRef;
        public static readonly MethodInfo SetMethodInfoRef;
        public static readonly MethodInfo SetInterfaceMethodInfoRef;
        public static readonly MethodInfo GetMethodAddressForTypeRef;
        public static readonly MethodInfo GetMethodAddressForInterfaceTypeRef;
        public static readonly MethodInfo IsInstanceRef;

        public static readonly FieldInfo mTypesRef;

        static VTablesImplRefs()
        {
            var vTablesImplType = CompilerEngine.MetadataContext.ImportType(typeof(VTablesImpl));

            foreach (var xField in typeof(VTablesImplRefs).GetFields().Where(f => f.FieldType == typeof(MethodInfo)))
            {
                if (xField.Name.EndsWith("Ref"))
                {
                    var xTempMethod = vTablesImplType.Methods.SingleOrDefault(
                        m => m.Name == xField.Name.Substring(0, xField.Name.Length - "Ref".Length));

                    if (xTempMethod == null)
                    {
                        throw new Exception("Method '" + xField.Name.Substring(0, xField.Name.Length - "Ref".Length) + "' not found on VTablesImpl!");
                    }

                    xField.SetValue(null, xTempMethod);
                }
            }

            mTypesRef = vTablesImplType.Fields.Single(f => f.Name == "mTypes");
        }
    }
}
