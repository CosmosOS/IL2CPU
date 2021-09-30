﻿using System;
using System.Reflection;
using Cosmos.IL2CPU.Extensions;
using IL2CPU.Reflection;

namespace Cosmos.IL2CPU
{
    public static class VTablesImplRefs
    {
        public static readonly Assembly RuntimeAssemblyDef;
        public static readonly Type VTablesImplDef;
        public static readonly MethodBase SetTypeInfoRef;
        public static readonly MethodBase SetInterfaceInfoRef;
        public static readonly MethodBase SetMethodInfoRef;
        public static readonly MethodBase SetInterfaceMethodInfoRef;
        public static readonly MethodBase GetMethodAddressForTypeRef;
        public static readonly MethodBase GetMethodAddressForInterfaceTypeRef;
        public static readonly MethodBase GetDeclaringTypeOfMethodForTypeRef;
        public static readonly MethodBase IsInstanceRef;
        public static readonly MethodBase GetBaseTypeRef;

        public static Func<Type, uint> GetTypeId;

        static VTablesImplRefs()
        {
            VTablesImplDef = Base.VTablesImpl;
            foreach (FieldInfo xField in typeof(VTablesImplRefs).GetFields())
            {
                if (xField.Name.EndsWith("Ref"))
                {
                    MethodBase xTempMethod = VTablesImplDef.GetMethod(xField.Name.Substring(0, xField.Name.Length - "Ref".Length));
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
