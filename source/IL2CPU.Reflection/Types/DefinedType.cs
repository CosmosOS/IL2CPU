using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

using IL2CPU.Reflection.Internal;

namespace IL2CPU.Reflection.Types
{
    public class DefinedType : TypeInfo
    {
        public override ModuleInfo Module => ResolvedDefinition.Module;

        public override int MetadataToken => ResolvedDefinition.MetadataToken;

        public override string Namespace => ResolvedDefinition.Namespace;
        public override string Name => ResolvedDefinition.Name;

        public override TypeInfo DeclaringType { get; }
        public override TypeInfo BaseType => _baseType.Value;

        public override IReadOnlyList<GenericParameter> GenericParameters => _genericParameters.Value;
        public override int GenericParameterCount => ResolvedDefinition.GenericParameterCount;

        public override IReadOnlyList<TypeInfo> GenericArguments
        {
            get
            {
                if (_genericArguments == null)
                {
                    if (GenericParameterCount == 0)
                    {
                        _genericArguments = Array.Empty<TypeInfo>();
                    }
                    else
                    {
                        _genericArguments = new TypeInfo[GenericParameterCount];
                    }
                }

                return _genericArguments;
            }
        }

        public override IReadOnlyCollection<TypeInfo> ExplicitImplementedInterfaces => _explicitImplementedInterfaces.Value;

        public override IReadOnlyCollection<TypeInfo> NestedTypes => _nestedTypes.Value;

        public override IReadOnlyList<EventInfo> Events => _events.Value;
        public override IReadOnlyList<FieldInfo> Fields => _fields.Value;
        public override IReadOnlyList<MethodInfo> Methods => _methods.Value;
        public override IReadOnlyList<PropertyInfo> Properties => _properties.Value;

        public override StructLayoutAttribute StructLayoutAttribute
        {
            get
            {
                var classLayout = ResolvedDefinition.GetClassLayout();
                var layoutKind = LayoutKind.Sequential;

                switch (Attributes & TypeAttributes.LayoutMask)
                {
                    case TypeAttributes.AutoLayout:
                        layoutKind = LayoutKind.Auto;
                        break;
                    case TypeAttributes.ExplicitLayout:
                        layoutKind = LayoutKind.Explicit;
                        break;
                    case TypeAttributes.SequentialLayout:
                        layoutKind = LayoutKind.Sequential;
                        break;
                    default:
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
                        throw new Exception("Internal error!");
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations
                }

                var charSet = Attributes.HasFlag(TypeAttributes.UnicodeClass) ? CharSet.Unicode : CharSet.Ansi;

                return new StructLayoutAttribute(layoutKind)
                {
                    Pack = classLayout.PackingSize,
                    Size = classLayout.Size
                };
            }
        }

        public override IReadOnlyCollection<CustomAttributeInfo> CustomAttributes => ResolvedDefinition.CustomAttributes;

        public TypeAttributes Attributes => ResolvedDefinition.Attributes;

        #region Type attribute wrappers

        public bool IsAbstract => HasFlag(TypeAttributes.Abstract);
        public bool IsAnsiClass => HasFlag(TypeAttributes.StringFormatMask, TypeAttributes.AnsiClass);
        public bool IsAutoClass => HasFlag(TypeAttributes.StringFormatMask, TypeAttributes.AutoClass);
        public bool IsAutoLayout => HasFlag(TypeAttributes.LayoutMask, TypeAttributes.AutoLayout);
        public bool IsBeforeFieldInit => HasFlag(TypeAttributes.BeforeFieldInit);
        public override bool IsClass => HasFlag(TypeAttributes.ClassSemanticsMask, TypeAttributes.Class) && !IsValueType;
        public bool IsCustomFormatClass => HasFlag(TypeAttributes.StringFormatMask, TypeAttributes.CustomFormatClass);
        public bool IsExplicitLayout => HasFlag(TypeAttributes.LayoutMask, TypeAttributes.ExplicitLayout);
        public bool HasSecurity => HasFlag(TypeAttributes.HasSecurity);
        public bool IsImport => HasFlag(TypeAttributes.Import);
        public override bool IsInterface => HasFlag(TypeAttributes.ClassSemanticsMask, TypeAttributes.Interface);
        public bool IsNestedAssembly => HasFlag(TypeAttributes.VisibilityMask, TypeAttributes.NestedAssembly);
        public bool IsNestedFamANDAssem => HasFlag(TypeAttributes.VisibilityMask, TypeAttributes.NestedFamANDAssem);
        public bool IsNestedFamily => HasFlag(TypeAttributes.VisibilityMask, TypeAttributes.NestedFamily);
        public bool IsNestedFamORAssem => HasFlag(TypeAttributes.VisibilityMask, TypeAttributes.NestedFamORAssem);
        public bool IsNestedPrivate => HasFlag(TypeAttributes.VisibilityMask, TypeAttributes.NestedPrivate);
        public bool IsNestedPublic => HasFlag(TypeAttributes.VisibilityMask, TypeAttributes.NestedPublic);
        public bool IsNotPublic => HasFlag(TypeAttributes.VisibilityMask, TypeAttributes.NotPublic);
        public bool IsPublic => HasFlag(TypeAttributes.VisibilityMask, TypeAttributes.Public);
        public bool IsRTSpecialName => HasFlag(TypeAttributes.RTSpecialName);
        public bool IsSealed => HasFlag(TypeAttributes.Sealed);
        public bool IsSequentialLayout => HasFlag(TypeAttributes.LayoutMask, TypeAttributes.SequentialLayout);
        public bool IsSerializable => HasFlag(TypeAttributes.Serializable);
        public bool IsSpecialName => HasFlag(TypeAttributes.SpecialName);
        public bool IsUnicodeClass => HasFlag(TypeAttributes.StringFormatMask, TypeAttributes.UnicodeClass);
        public bool IsWindowsRuntime => HasFlag(TypeAttributes.WindowsRuntime);

        #endregion

        public override bool IsGenericType => GenericParameterCount != 0;
        
        internal virtual ResolvedTypeDefinition ResolvedDefinition { get; }
        internal override GenericContext GenericContext { get; }

        private protected override TypeKind Kind => TypeKind.Defined;

        private protected override IReadOnlyCollection<MethodImpl> MethodImplementations => _methodImplementations.Value;

        private readonly Lazy<TypeInfo> _baseType;

        private readonly Lazy<IReadOnlyList<GenericParameter>> _genericParameters;

        private readonly Lazy<IReadOnlyCollection<TypeInfo>> _explicitImplementedInterfaces;
        private readonly Lazy<IReadOnlyCollection<MethodImpl>> _methodImplementations;

        private readonly Lazy<IReadOnlyList<DefinedType>> _nestedTypes;

        private readonly Lazy<IReadOnlyList<MethodInfo>> _methods;
        private readonly Lazy<IReadOnlyList<FieldInfo>> _fields;
        private readonly Lazy<IReadOnlyList<PropertyInfo>> _properties;
        private readonly Lazy<IReadOnlyList<EventInfo>> _events;

        private IReadOnlyList<TypeInfo> _genericArguments;

        protected DefinedType()
        {
            GenericContext = GenericContext.Empty;

            _baseType = new Lazy<TypeInfo>(ResolveBaseType);

            _genericParameters = new Lazy<IReadOnlyList<GenericParameter>>(ResolveGenericParameters);

            _explicitImplementedInterfaces = new Lazy<IReadOnlyCollection<TypeInfo>>(ResolveExplicitImplementedInterfaces);
            _methodImplementations = new Lazy<IReadOnlyCollection<MethodImpl>>(ResolveMethodImplementations);

            _nestedTypes = new Lazy<IReadOnlyList<DefinedType>>(ResolveNestedTypes);

            _methods = new Lazy<IReadOnlyList<MethodInfo>>(ResolveMethods);
            _fields = new Lazy<IReadOnlyList<FieldInfo>>(ResolveFields);
            _properties = new Lazy<IReadOnlyList<PropertyInfo>>(ResolveProperties);
            _events = new Lazy<IReadOnlyList<EventInfo>>(ResolveEvents);
        }

        internal DefinedType(
            ResolvedTypeDefinition resolvedTypeDefinition,
            TypeInfo declaringType,
            IReadOnlyList<TypeInfo> typeArguments)
            : this()
        {
            ResolvedDefinition = resolvedTypeDefinition;

            DeclaringType = declaringType;

            GenericContext = DeclaringType?.GenericContext ?? GenericContext.Empty;

            if (typeArguments != null
                && typeArguments.Count > 0)
            {
                _genericArguments = typeArguments;
                GenericContext = GenericContext.WithTypeArguments(typeArguments);
            }
            else if (GenericParameterCount > 0)
            {
                _genericArguments = new TypeInfo[GenericParameterCount];
            }
        }

        public override TypeInfo MakeGenericType(IReadOnlyList<TypeInfo> typeArguments)
        {
            if (!IsGenericType)
            {
                throw new InvalidOperationException("MakeGenericType can only be called on generic method definitions!");
            }

#pragma warning disable CA1062 // Validate arguments of public methods
            if (GenericParameters.Count != typeArguments.Count)
#pragma warning restore CA1062 // Validate arguments of public methods
            {
                throw new InvalidOperationException("The type argument count should be the same as the generic parameter count!");
            }

            return new DefinedType(ResolvedDefinition, DeclaringType, typeArguments);
        }

        public override TypeInfo GetGenericTypeDefinition()
        {
            if (IsGenericTypeDefinition)
            {
                return this;
            }

            return new DefinedType(ResolvedDefinition, DeclaringType, null);
        }

        public override int GetHashCode() => ResolvedDefinition.GetHashCode();

        public override bool Equals(object obj) =>
            obj is DefinedType type
            && type.ResolvedDefinition == ResolvedDefinition
            && type.GenericArguments.SequenceEqual(GenericArguments);

        #region Internal resolution

        internal DefinedType ResolveNestedType(string typeName)
        {
            foreach (var type in NestedTypes)
            {
                if (type.Name == typeName)
                {
                    return (DefinedType)type;
                }
            }

            throw new Exception("Internal error!");
        }

        internal MethodInfo ResolveMethod(string name, MethodSignature<TypeInfo> signature)
        {
            foreach (var method in Methods)
            {
                if (method.Matches(name, signature))
                {
                    return method;
                }
            }

            throw new Exception("Internal error!");
        }

        internal FieldInfo ResolveField(string name, TypeInfo signature)
        {
            foreach (var field in Fields)
            {
                if (field.Matches(name, signature))
                {
                    return field;
                }
            }

            throw new Exception("Internal error!");
        }

        #endregion

        private IReadOnlyList<GenericParameter> ResolveGenericParameters() =>
            ResolvedDefinition.ResolveGenericParameters(GenericContext);

        private IReadOnlyCollection<TypeInfo> ResolveExplicitImplementedInterfaces() =>
            ResolvedDefinition.ResolveImplementedInterfaces(GenericContext);

        private IReadOnlyCollection<MethodImpl> ResolveMethodImplementations() =>
            ResolvedDefinition.ResolveMethodImplementations(GenericContext);

        private IReadOnlyList<DefinedType> ResolveNestedTypes() =>
            ResolveMembers(ResolvedDefinition.NestedTypes, d => new DefinedType(d, this, null));

        private IReadOnlyList<MethodInfo> ResolveMethods() =>
            ResolveMembers(ResolvedDefinition.Methods, d => new MethodInfo(d, this, null));

        private IReadOnlyList<FieldInfo> ResolveFields() =>
            ResolveMembers(ResolvedDefinition.Fields, d => new FieldInfo(d, this));

        private IReadOnlyList<PropertyInfo> ResolveProperties() =>
            ResolveMembers(ResolvedDefinition.Properties, d => new PropertyInfo(d, this));

        private IReadOnlyList<EventInfo> ResolveEvents() =>
            ResolveMembers(ResolvedDefinition.Events, d => new EventInfo(d, this));

        private TypeInfo ResolveBaseType() => ResolvedDefinition.ResolveBaseType(GenericContext);

        private static IReadOnlyList<TMember> ResolveMembers<TDefinition, TMember>(
            IReadOnlyList<TDefinition> definitions, Func<TDefinition, TMember> createMember)
        {
            var members = new List<TMember>(definitions.Count);

            foreach (var definition in definitions)
            {
                members.Add(createMember(definition));
            }

            return members;
        }

        private bool HasFlag(TypeAttributes flag) => (Attributes & flag) != 0;
        private bool HasFlag(TypeAttributes mask, TypeAttributes flag) => (Attributes & mask) == flag;
    }
}
