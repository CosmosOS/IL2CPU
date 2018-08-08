using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;

using IL2CPU.Reflection.Internal;
using IL2CPU.Reflection.Types;

namespace IL2CPU.Reflection
{
    public abstract class TypeInfo : MemberInfo
    {
        public override int MetadataToken => 0;

#pragma warning disable CA1716 // Identifiers should not match keywords
        public abstract string Namespace { get; }
#pragma warning restore CA1716 // Identifiers should not match keywords
        public abstract string Name { get; }

        public string FullName => _fullName.Value;

        public string AssemblyQualifiedName => $"{FullName}, {Assembly.Identity.FullName}";

        public abstract TypeInfo BaseType { get; }

        public virtual IReadOnlyList<GenericParameter> GenericParameters => Array.Empty<GenericParameter>();
        public virtual int GenericParameterCount => GenericParameters.Count;

        public virtual IReadOnlyList<TypeInfo> GenericArguments => Array.Empty<TypeInfo>();

        public bool ContainsGenericParameters => GenericArguments.Any(a => a == null);

        public virtual IReadOnlyCollection<TypeInfo> ExplicitImplementedInterfaces => Array.Empty<TypeInfo>();

        public virtual IReadOnlyCollection<TypeInfo> NestedTypes => Array.Empty<TypeInfo>();

        public virtual IReadOnlyList<EventInfo> Events => Array.Empty<EventInfo>();
        public virtual IReadOnlyList<FieldInfo> Fields => Array.Empty<FieldInfo>();
        public virtual IReadOnlyList<MethodInfo> Methods => Array.Empty<MethodInfo>();
        public virtual IReadOnlyList<PropertyInfo> Properties => Array.Empty<PropertyInfo>();

        public virtual StructLayoutAttribute StructLayoutAttribute => null;

        public override IReadOnlyCollection<CustomAttributeInfo> CustomAttributes => Array.Empty<CustomAttributeInfo>();

        public bool IsArray => Kind == TypeKind.Array;
        public bool IsByReference => Kind == TypeKind.ByReference;
        public bool IsDefined => Kind == TypeKind.Defined;
        public bool IsPinned => Kind == TypeKind.Pinned;
        public bool IsPointer => Kind == TypeKind.Pointer;
        public bool IsSZArray => Kind == TypeKind.SZArray;

        public abstract bool IsClass { get; }
        public abstract bool IsInterface { get; }

        public abstract bool IsGenericType { get; }
        public bool IsGenericTypeDefinition => IsGenericType && GenericArguments.All(a => a == null);

        public bool IsEnum => BaseType != null && BaseType.IsEnum();
        public bool IsValueType => BaseType != null && BaseType.IsValueType() || IsEnum;

        public bool IsPrimitive =>
            DeclaringType == null
            && Namespace == "System"
            && (Name == "Boolean"
             || Name == "Byte"
             || Name == "Char"
             || Name == "Double"
             || Name == "Int16"
             || Name == "Int32"
             || Name == "Int64"
             || Name == "IntPtr"
             || Name == "SByte"
             || Name == "Single"
             || Name == "UInt16"
             || Name == "UInt32"
             || Name == "UInt64"
             || Name == "UIntPtr");

        internal virtual GenericContext GenericContext => GenericContext.Empty;

#warning todo: use for signature match
        internal IReadOnlyCollection<(TypeInfo Modifier, bool IsRequired)> Modifiers => _modifiers;

        private protected abstract TypeKind Kind { get; }

        private protected virtual IReadOnlyCollection<MethodImpl> MethodImplementations => BaseType?.MethodImplementations;

        private readonly Lazy<string> _fullName;

        private Dictionary<MethodInfo, MethodInfo> _interfaceTable;

        private List<(TypeInfo Modifier, bool IsRequired)> _modifiers = new List<(TypeInfo Modifier, bool IsRequired)>();

        private protected TypeInfo()
        {
            _fullName = new Lazy<string>(BuildFullName);
        }

        public SZArrayType MakeArrayType() => new SZArrayType(this);
        public ArrayType MakeArrayType(int rank) => new ArrayType(
            this, new ArrayShape(rank, ImmutableArray<int>.Empty, ImmutableArray<int>.Empty));

        public ByReferenceType MakeByReferenceType() => new ByReferenceType(this);
        public PointerType MakePointerType() => new PointerType(this);

        public TypeInfo MakeGenericType(params TypeInfo[] typeArguments) =>
            MakeGenericType((IReadOnlyList<TypeInfo>)typeArguments);

        public virtual TypeInfo MakeGenericType(IReadOnlyList<TypeInfo> typeArguments) =>
            throw new InvalidOperationException();

        public IReadOnlyCollection<(MethodInfo InterfaceMethod, MethodInfo TargetMethod)> GetInterfaceMapping(
            TypeInfo interfaceType)
        {
            if (interfaceType == null)
            {
                throw new ArgumentNullException(nameof(interfaceType));
            }

            var interfaceMethods = interfaceType.Methods;
            var mappings = new List<(MethodInfo InterfaceMethod, MethodInfo TargetMethod)>(interfaceMethods.Count);

            foreach (var method in interfaceMethods)
            {
                mappings.Add((method, FindTargetMethodForInterfaceMethod(method)));
            }

            return mappings;
        }

        public virtual TypeInfo GetGenericTypeDefinition() => this;

        public override int GetHashCode() => FullName.GetHashCode();

        public override bool Equals(object obj) =>
            obj is TypeInfo type
            && type.AssemblyQualifiedName == AssemblyQualifiedName;

        public override string ToString() => FullName;

        internal TypeInfo MakeModifiedType(TypeInfo modifier, bool isRequired)
        {
            var modifiedType = (TypeInfo)MemberwiseClone();
            modifiedType._modifiers.Add((modifier, isRequired));

            return modifiedType;
        }

        private string BuildFullName()
        {
            if (!IsGenericTypeDefinition && ContainsGenericParameters)
            {
                return null;
            }

            var builder = new StringBuilder();

            if (DeclaringType != null)
            {
                builder.Append(DeclaringType.FullName);
                builder.Append('+');
            }

            if (Namespace != null)
            {
                builder.Append(Namespace);
                builder.Append('.');
            }

            builder.Append(Name);

            if (IsGenericType)
            {
                if (!IsGenericTypeDefinition)
                {
                    builder.Append('<');
                    builder.Append(String.Join(", ", GenericArguments));
                    builder.Append('>');
                }
            }

            return builder.ToString();
        }

        private MethodInfo FindTargetMethodForInterfaceMethod(MethodInfo interfaceMethod)
        {
            if (IsSZArray
                && interfaceMethod.DeclaringType.IsGenericType)
            {
                throw new Exception("Interface mappings for generic interfaces cannot be retrieved for arrays!");
            }

            if (ImplementsInterface(interfaceMethod.DeclaringType))
            {
                var methodImplementation = FindExplicitMethodImplementation(interfaceMethod)
                    ?? FindImplicitMethodImplementation(interfaceMethod);

                if (methodImplementation != null)
                {
                    return methodImplementation;
                }
            }

            if (BaseType != null)
            {
                return BaseType.FindTargetMethodForInterfaceMethod(interfaceMethod);
            }

            if (this is DefinedType definedType && definedType.IsAbstract)
            {
                return null;
            }

            throw new Exception("Internal error!");
        }

        private MethodInfo FindExplicitMethodImplementation(MethodInfo interfaceMethod)
        {
            if (MethodImplementations != null)
            {
                foreach (var methodImpl in MethodImplementations)
                {
                    if (methodImpl.MethodDeclaration == interfaceMethod)
                    {
                        foreach (var method in Methods)
                        {
                            if (method.ResolvedDefinition == methodImpl.MethodBody.ResolvedDefinition)
                            {
                                return method;
                            }
                        }
                    }
                }
            }

            return null;
        }

        private MethodInfo FindImplicitMethodImplementation(MethodInfo interfaceMethod)
        {
            foreach (var method in Methods.Where(m => m.IsPublic))
            {
                if (method.Matches(interfaceMethod))
                {
                    return method;
                }
            }

            return null;
        }

        private bool ImplementsInterface(TypeInfo interfaceType)
        {
            foreach (var explicitInterface in ExplicitImplementedInterfaces)
            {
                if (explicitInterface == interfaceType)
                {
                    return true;
                }

                if (explicitInterface.ImplementsInterface(interfaceType))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool operator ==(TypeInfo typeInfo, object obj)
        {
            if (typeInfo is null)
            {
                if (obj == null)
                {
                    return true;
                }

                return false;
            }

            if (obj == null)
            {
                return false;
            }

            if (obj is TypeInfo other)
            {
                return EqualityComparer<TypeInfo>.Default.Equals(typeInfo, other);
            }
            else if (obj is Type type)
            {
#pragma warning disable CA1062 // Validate arguments of public methods
                return typeInfo.Namespace == type.Namespace
#pragma warning restore CA1062 // Validate arguments of public methods
                    && typeInfo.Name == type.Name
                    && typeInfo.DeclaringType == type.DeclaringType
                    && typeInfo.Module.Name == type.GetTypeInfo().Module.Name;
            }

            return false;
        }

        public static bool operator !=(TypeInfo typeInfo, object obj) => !(typeInfo == obj);
    }
}
