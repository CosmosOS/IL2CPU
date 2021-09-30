using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace IL2CPU.Reflection
{
    public static class TypeExtensions
    {
        private static readonly BindingFlags _all = BindingFlags.Instance |
                                                    BindingFlags.Public | BindingFlags.NonPublic;

        public static InterfaceMapping GetMyInterfaceMap(this Type target, Type interfaceType)
        {
            if (!interfaceType.IsInterface || !target.GetInterfaces().Contains(interfaceType))
            {
                throw new ArgumentException(target.FullName, nameof(interfaceType));
            }
            var args = target.GetGenericArguments();
            var mod = target.Module;
            var reader = mod.GetReader();
            var th = MetadataTokens.Handle(target.MetadataToken);
            var typeDef = reader.GetTypeDefinition((TypeDefinitionHandle)th);
            var impl = typeDef.GetMethodImplementations()
                .Select(g =>
                {
                    var i = reader.GetMethodImplementation(g);
                    return new
                    {
                        typeToken = MetadataTokens.GetToken(i.Type),
                        intf = mod.ResolveMyMethod(MetadataTokens.GetToken(i.MethodDeclaration), args),
                        real = MetadataTokens.GetToken(i.MethodBody)
                    };
                })
                .Where(g => g.typeToken == target.MetadataToken)
                .GroupBy(g => g.intf.MetadataToken)
                .ToDictionary(k => k.Key, v => v.ToArray());
            var interfMethods = interfaceType.GetMethods(_all);
            var targetMethods = target.GetMethods(_all);
            var interfList = new List<MethodInfo>();
            var targetList = new List<MethodInfo>();
            foreach (var im in interfMethods)
            {
                MethodInfo method = null;
                if (impl.TryGetValue(im.MetadataToken, out var found))
                {
                    foreach (var find in found)
                    {
                        var maybe = targetMethods.First(m => m.MetadataToken == find.real);
                        if (maybe.ReturnType != im.ReturnType)
                            continue;
                        method = maybe;
                    }
                }
                if (method == null)
                {
                    var pTypes = im.GetParameters().Select(p => p.ParameterType).ToArray();
                    method = target.GetMethod(im.Name, _all, null, pTypes, null);
                }
                if (method == null)
                {
                    continue;
                }
                interfList.Add(im);
                targetList.Add(method);
            }
            var r = new InterfaceMapping
            {
                InterfaceMethods = interfList.ToArray(),
                InterfaceType = interfaceType,
                TargetMethods = targetList.ToArray(),
                TargetType = target
            };
            return r;
        }

        public static string GetNestedName(this Type type)
        {
            string fullName;
            if (type.IsGenericParameter)
            {
                fullName = type.Name;
            }
            else
            {
                fullName = type.Namespace + "." + type.Name;
            }
            if (type.IsNested && !type.IsGenericParameter)
            {
                fullName = GetNestedName(type.DeclaringType) + "+" + type.Name;
            }
            return fullName;
        }
    }
}
