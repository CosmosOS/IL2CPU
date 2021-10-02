using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace IL2CPU.Reflection
{
    public static class ModuleExtensions
    {
        private static readonly BindingFlags _All = BindingFlags.Public | BindingFlags.NonPublic |
                                                    BindingFlags.Static | BindingFlags.Instance;

        private static readonly BindingFlags _Static = BindingFlags.Public | BindingFlags.NonPublic |
                                                              BindingFlags.Static;

        private static readonly BindingFlags _This = BindingFlags.Public | BindingFlags.NonPublic |
                                                              BindingFlags.Instance;

        public static FieldInfo ResolveMyField(this Module module, int metadataToken,
            Type[] genTypeArgs = null, Type[] genMethArgs = null)
        {
            var fieldHandle = MetadataTokens.Handle(metadataToken);
            if (fieldHandle.Kind == HandleKind.FieldDefinition)
            {
                var allFields = module.GetTypes()
                    .SelectMany(t => t.GetFields(_All));
                var metaField = allFields.FirstOrDefault(m => m.Module == module
                                                              && m.MetadataToken == metadataToken);
                if (metaField != null)
                {
                    return metaField;
                }
            }
            if (fieldHandle.Kind == HandleKind.MemberReference)
            {
                var ctx = ToContext(genTypeArgs, genMethArgs);
                var reader = GetReader(module);
                var member = reader.GetMemberReference((MemberReferenceHandle)fieldHandle);
                var (fqn, ass, typ) = Translate(module, member.Parent, ctx);
                Type memberOwner;
                if (typ == null)
                {
                    var loader = GetLoader(module);
                    var found = loader.LoadFromAssemblyName(ass);
                    memberOwner = FindType(found, fqn, ass.Name);
                }
                else
                {
                    memberOwner = typ;
                    ctx.TypeParameters = typ.GetGenericArguments();
                }
                var memberName = reader.GetString(member.Name);
                var fields = memberOwner.GetFields(_All)
                    .Where(m => m.Name == memberName)
                    .ToArray();
                if (fields.Length == 1)
                {
                    return fields[0];
                }
            }
            throw new InvalidOperationException(module + " " + metadataToken);
        }

        public static MethodBase ResolveMyMethod(this Module module, int metadataToken,
            Type[] genTypeArgs = null, Type[] genMethArgs = null)
        {
            var methodHandle = MetadataTokens.Handle(metadataToken);
            if (methodHandle.Kind == HandleKind.MethodDefinition)
            {
                var allMethods = module.GetTypes()
                    .SelectMany(t => t.GetMethods(_All)
                        .Concat<MethodBase>(t.GetConstructors(_All))
                    );
                var metaMethod = allMethods.FirstOrDefault(m => m.Module == module
                                                                && m.MetadataToken == metadataToken);
                if (metaMethod != null)
                {
                    return metaMethod;
                }
            }
            if (methodHandle.Kind == HandleKind.MemberReference)
            {
                var ctx = ToContext(genTypeArgs, genMethArgs);
                var reader = GetReader(module);
                var member = reader.GetMemberReference((MemberReferenceHandle)methodHandle);
                var (fqn, ass, typ) = Translate(module, member.Parent, ctx);
                Type memberOwner;
                if (typ == null)
                {
                    var loader = GetLoader(module);
                    var found = loader.LoadFromAssemblyName(ass);
                    memberOwner = FindType(found, fqn, ass.Name);
                }
                else
                {
                    memberOwner = typ;
                    ctx.TypeParameters = typ.GetGenericArguments();
                }
                var memberName = reader.GetString(member.Name);
                var sig = member.DecodeMethodSignature(GetProvider(module), ctx);
                var directMatch = FindMethod(memberOwner, memberName, sig, ctx);
                if (directMatch != null)
                {
                    return directMatch;
                }
            }
            if (methodHandle.Kind == HandleKind.MethodSpecification)
            {
                var reader = GetReader(module);
                var owner = reader.GetMethodSpecification((MethodSpecificationHandle)methodHandle);
                var ctx = ToContext(genTypeArgs, genMethArgs);
                var methTypes = owner.DecodeSignature(GetProvider(module), ctx);
                ctx.MethodParameters = methTypes.ToArray();
                var baseToken = owner.Method.GetHashCode();
                var baseMeth = ResolveMyMethod(module, baseToken, ctx.TypeParameters, ctx.MethodParameters);
                if (baseMeth.IsGenericMethodDefinition && baseMeth is MethodInfo bm)
                {
                    baseMeth = bm.MakeGenericMethod(ctx.MethodParameters);
                }
                return baseMeth;
            }
            throw new InvalidOperationException(module + " " + metadataToken);
        }

        private static MethodBase FindMethod(Type owner, string name, MethodSignature<Type> sig, SomeGenerics ctx)
        {
            var flags = sig.Header.IsInstance ? _This : _Static;
            var parmTypes = sig.ParameterTypes.ToArray();
            MethodBase method;
            try
            {
                method = owner.GetMethod(name, flags, null, parmTypes, null);
            }
            catch (AmbiguousMatchException)
            {
                // Maybe multiple return types matching?!
                var multi = new MultipleReturnTypeBinder(sig, ctx);
                method = owner.GetMethod(name, flags, multi, parmTypes, null);
            }
            if (method == null && IsConstructor(name))
            {
                method = owner.GetConstructor(flags, null, parmTypes, null);
            }
            if (method != null && sig.Header.IsGeneric && !method.ContainsGenericParameters)
            {
                // No generic method found, although it was expected?!
                method = null;
            }
            if (method == null && sig.GenericParameterCount >= 1)
            {
                var methods = owner.GetMethods(flags)
                    .Where(m => m.Name == name
                                && m.ContainsGenericParameters
                                && m.GetParameters().Length == sig.ParameterTypes.Length)
                    .Select(m => AsGeneric(m, ctx))
                    .Where(m => m != null)
                    .ToArray();
                if (methods.Length == 1)
                {
                    return methods[0];
                }
                throw new InvalidOperationException(String.Join(Environment.NewLine,
                    methods.Select(m => m.ToString())));
            }
            if (method != null && sig.GenericParameterCount >= 1)
            {
                method = AsGeneric((MethodInfo)method, ctx);
            }
            return method;
        }

        private static bool IsConstructor(string name)
        {
            return name == ".ctor" || name == ".cctor";
        }

        private static MethodInfo AsGeneric(MethodInfo method, SomeGenerics ctx)
        {
            var parms = ctx.MethodParameters;
            var genMeth = method.MakeGenericMethod(parms);
            return genMeth;
        }

        private static SomeGenerics ToContext(Type[] genTypeArgs, Type[] genMethArgs)
        {
            var ctx = new SomeGenerics { TypeParameters = genTypeArgs, MethodParameters = genMethArgs };
            return ctx;
        }

        private static Type FindType(Assembly handle, string typeName, string assName)
        {
            var type = handle.GetType(typeName);
            if (type != null)
            {
                return type;
            }
            if (assName == "netstandard")
            {
                assName = "System.Private.CoreLib";
            }
            var fqn = typeName + ", " + assName;
            type = handle.GetType(fqn);
            return type;
        }

        private static (string, AssemblyName, Type) Translate(Module module, EntityHandle entity, SomeGenerics ctx)
        {
            var reader = GetReader(module);
            if (entity.Kind == HandleKind.TypeReference)
            {
                var owner = reader.GetTypeReference((TypeReferenceHandle)entity);
                var ownerNameSp = reader.GetString(owner.Namespace);
                var ownerName = reader.GetString(owner.Name);
                var ownerFqn = ownerNameSp + '.' + ownerName;
                var ownerScope = owner.ResolutionScope;
                if (ownerScope.Kind == HandleKind.AssemblyReference)
                {
                    var ar = reader.GetAssemblyReference((AssemblyReferenceHandle)ownerScope);
                    var arName = ar.GetAssemblyName();
                    return (ownerFqn, arName, null);
                }
                if (ownerScope.Kind == HandleKind.TypeReference || ownerScope.Kind == HandleKind.TypeSpecification)
                {
                    var scoped = MetadataTokens.GetToken(ownerScope);
                    var parentType = ResolveMyType(module, scoped, ctx.TypeParameters, ctx.MethodParameters);
                    var nestedFqn = parentType.FullName + '+' + ownerFqn.TrimStart('.');
                    var arName = parentType.Assembly.GetName();
                    return (nestedFqn, arName, null);
                }
            }
            if (entity.Kind == HandleKind.TypeSpecification)
            {
                var owner = reader.GetTypeSpecification((TypeSpecificationHandle)entity);
                var specType = owner.DecodeSignature(GetProvider(module), ctx);
                return (null, null, specType);
            }
            throw new InvalidOperationException(entity.Kind + " ?");
        }

        public static Type ResolveMyType(this Module module, int metadataToken,
            Type[] genTypeArgs = null, Type[] genMethArgs = null)
        {
            var typeHandle = MetadataTokens.Handle(metadataToken);
            if (typeHandle.Kind == HandleKind.TypeDefinition)
            {
                var allTypes = module.GetTypes();
                var metaType = allTypes.FirstOrDefault(m => m.Module == module
                                                            && m.MetadataToken == metadataToken);
                if (metaType != null)
                {
                    return metaType;
                }
            }
            if (typeHandle.Kind == HandleKind.TypeReference || typeHandle.Kind == HandleKind.TypeSpecification)
            {
                var ctx = ToContext(genTypeArgs, genMethArgs);
                var (fqn, ass, typ) = Translate(module, (EntityHandle)typeHandle, ctx);
                Type owner;
                if (typ == null)
                {
                    var loader = GetLoader(module);
                    var found = loader.LoadFromAssemblyName(ass);
                    owner = FindType(found, fqn, ass.Name);
                }
                else
                {
                    owner = typ;
                    ctx.TypeParameters = typ.GetGenericArguments();
                }
                if (owner != null)
                {
                    return owner;
                }
            }
            throw new InvalidOperationException(module + " " + metadataToken);
        }

        public static MetadataReader GetReader(this Module module, bool shouldThrow = true)
        {
            var reader = (MetadataReader)module.GetType()
                .GetProperty("Reader", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(module);
            if (reader == null && shouldThrow)
            {
                throw new InvalidOperationException(module.Assembly.Location);
            }
            return reader;
        }

        public static MetadataLoadContext GetLoader(this Module module, bool shouldThrow = true)
        {
            var loader = (MetadataLoadContext)module.GetType()
                .GetProperty("Loader", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(module);
            if (loader == null && shouldThrow)
            {
                throw new InvalidOperationException(module.Assembly.Location);
            }
            return loader;
        }

        private static SomeTypeProvider GetProvider(Module module)
        {
            var provider = new SomeTypeProvider(module, BaseTypeSystem.BaseTypes);
            return provider;
        }

        public static string ResolveMyString(this Module module, int metadataToken)
        {
            var reader = GetReader(module, shouldThrow: false);
            var stringHandle = (UserStringHandle)MetadataTokens.Handle(metadataToken);
            if (reader == null)
            {
                var txt = module.ResolveString(metadataToken);
                return txt;
            }
            var stringTxt = reader.GetUserString(stringHandle);
            return stringTxt;
        }
    }
}
