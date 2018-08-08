using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

using IL2CPU.Reflection.Internal;
using IL2CPU.Reflection.Types;

namespace IL2CPU.Reflection
{
    public sealed class ModuleInfo : ICustomAttributeProvider
    {
        public string Name => _name.Value;

        public AssemblyInfo Assembly { get; }
        public bool IsManifestModule => MetadataReader.IsAssembly;

        public string Location => _moduleReader.ModulePath;

        public IEnumerable<DefinedType> Types => _types.Value;

        /// <summary>
        /// Global methods.
        /// </summary>
        public IEnumerable<MethodInfo> Methods => _methods.Value;
        /// <summary>
        /// Global fields.
        /// </summary>
        public IEnumerable<FieldInfo> Fields => _fields.Value;

        public IReadOnlyCollection<CustomAttributeInfo> CustomAttributes => _customAttributes.Value;

        public MetadataContext MetadataContext { get; }

        internal TypeProvider TypeProvider { get; }

        internal PEReader PEReader => _moduleReader.PEReader;

        internal MetadataReader MetadataReader => _moduleReader.MetadataReader;
        internal MetadataReader PdbMetadataReader => _moduleReader.PdbMetadataReader;

        ModuleInfo ICustomAttributeProvider.Module => this;

        private readonly ModuleReader _moduleReader;

        private readonly ModuleDefinition _moduleDefinition;

        private readonly Lazy<string> _name;

        private readonly Lazy<IEnumerable<DefinedType>> _types;

        private readonly Lazy<IEnumerable<MethodInfo>> _methods;
        private readonly Lazy<IEnumerable<FieldInfo>> _fields;

        private readonly Lazy<IReadOnlyCollection<CustomAttributeInfo>> _customAttributes;

        #region Cache

        private readonly Dictionary<TypeDefinitionHandle, ResolvedTypeDefinition> _resolvedTypeDefinitions =
            new Dictionary<TypeDefinitionHandle, ResolvedTypeDefinition>();

        private readonly Dictionary<MethodDefinitionHandle, ResolvedMethodDefinition> _resolvedMethodDefinitions =
            new Dictionary<MethodDefinitionHandle, ResolvedMethodDefinition>();

        private readonly Dictionary<FieldDefinitionHandle, ResolvedFieldDefinition> _resolvedFieldDefinitions =
            new Dictionary<FieldDefinitionHandle, ResolvedFieldDefinition>();

        private readonly Dictionary<PropertyDefinitionHandle, ResolvedPropertyDefinition> _resolvedPropertyDefinitions =
            new Dictionary<PropertyDefinitionHandle, ResolvedPropertyDefinition>();

        private readonly Dictionary<EventDefinitionHandle, ResolvedEventDefinition> _resolvedEventDefinitions =
            new Dictionary<EventDefinitionHandle, ResolvedEventDefinition>();

        #endregion

        internal ModuleInfo(
            MetadataContext metadataContext,
            AssemblyInfo assembly,
            ModuleReader moduleReader)
        {
            Assembly = assembly;

            MetadataContext = metadataContext;
            TypeProvider = new TypeProvider(MetadataContext, this);

            _moduleReader = moduleReader;
            _moduleDefinition = MetadataReader.GetModuleDefinition();

            _name = new Lazy<string>(GetName);

            _types = new Lazy<IEnumerable<DefinedType>>(ResolveTypes);

            _methods = new Lazy<IEnumerable<MethodInfo>>(ResolveMethods);
            _fields = new Lazy<IEnumerable<FieldInfo>>(ResolveFields);

            _customAttributes = new Lazy<IReadOnlyCollection<CustomAttributeInfo>>(ResolveCustomAttributes);
        }

        public TypeInfo ResolveType(int metadataToken) => ResolveType(metadataToken, null, null);

        public TypeInfo ResolveType(
            int metadataToken,
            IReadOnlyList<TypeInfo> typeArguments,
            IReadOnlyList<TypeInfo> methodArguments) =>
            ResolveTypeHandle(MetadataTokens.EntityHandle(metadataToken), new GenericContext(typeArguments, methodArguments));

        public MethodInfo ResolveMethod(int metadataToken) => ResolveMethod(metadataToken, null, null);

        public MethodInfo ResolveMethod(
            int metadataToken,
            IReadOnlyList<TypeInfo> typeArguments,
            IReadOnlyList<TypeInfo> methodArguments) =>
            ResolveMethodHandle(MetadataTokens.EntityHandle(metadataToken), new GenericContext(typeArguments, methodArguments));

        public FieldInfo ResolveField(int metadataToken) => ResolveField(metadataToken, null, null);

        public FieldInfo ResolveField(
            int metadataToken,
            IReadOnlyList<TypeInfo> typeArguments,
            IReadOnlyList<TypeInfo> methodArguments) =>
            ResolveFieldHandle(MetadataTokens.EntityHandle(metadataToken), new GenericContext(typeArguments, methodArguments));

        #region Internal cached resolution

        internal ResolvedTypeDefinition ResolveTypeDefinitionInternal(
            TypeDefinitionHandle typeDefinitionHandle)
        {
            if (!_resolvedTypeDefinitions.TryGetValue(typeDefinitionHandle, out var resolvedDefinition))
            {
                resolvedDefinition = new ResolvedTypeDefinition(this, typeDefinitionHandle);
                _resolvedTypeDefinitions.Add(typeDefinitionHandle, resolvedDefinition);
            }

            return resolvedDefinition;
        }

        internal ResolvedMethodDefinition ResolveMethodDefinitionInternal(
            MethodDefinitionHandle methodDefinitionHandle)
        {
            if (!_resolvedMethodDefinitions.TryGetValue(methodDefinitionHandle, out var resolvedDefinition))
            {
                resolvedDefinition = new ResolvedMethodDefinition(this, methodDefinitionHandle);
                _resolvedMethodDefinitions.Add(methodDefinitionHandle, resolvedDefinition);
            }

            return resolvedDefinition;
        }

        internal ResolvedFieldDefinition ResolveFieldDefinitionInternal(
            FieldDefinitionHandle fieldDefinitionHandle)
        {
            if (!_resolvedFieldDefinitions.TryGetValue(fieldDefinitionHandle, out var resolvedDefinition))
            {
                resolvedDefinition = new ResolvedFieldDefinition(this, fieldDefinitionHandle);
                _resolvedFieldDefinitions.Add(fieldDefinitionHandle, resolvedDefinition);
            }

            return resolvedDefinition;
        }

        internal ResolvedPropertyDefinition ResolvePropertyDefinitionInternal(
            PropertyDefinitionHandle propertyDefinitionHandle)
        {
            if (!_resolvedPropertyDefinitions.TryGetValue(propertyDefinitionHandle, out var resolvedDefinition))
            {
                resolvedDefinition = new ResolvedPropertyDefinition(this, propertyDefinitionHandle);
                _resolvedPropertyDefinitions.Add(propertyDefinitionHandle, resolvedDefinition);
            }

            return resolvedDefinition;
        }

        internal ResolvedEventDefinition ResolveEventDefinitionInternal(
            EventDefinitionHandle eventDefinitionHandle)
        {
            if (!_resolvedEventDefinitions.TryGetValue(eventDefinitionHandle, out var resolvedDefinition))
            {
                resolvedDefinition = new ResolvedEventDefinition(this, eventDefinitionHandle);
                _resolvedEventDefinitions.Add(eventDefinitionHandle, resolvedDefinition);
            }

            return resolvedDefinition;
        }

        #endregion

        #region Internal generic handle resolution

        internal TypeInfo ResolveTypeHandle(EntityHandle typeHandle, GenericContext genericContext)
        {
            if (typeHandle.IsNil)
            {
                throw new ArgumentNullException(nameof(typeHandle));
            }

            switch (typeHandle.Kind)
            {
                case HandleKind.TypeDefinition:
                    return ResolveTypeDefinition((TypeDefinitionHandle)typeHandle);
                case HandleKind.TypeReference:
                    return new ReferencedDefinedType(this, (TypeReferenceHandle)typeHandle);
                case HandleKind.TypeSpecification:
                    return ResolveTypeSpecification((TypeSpecificationHandle)typeHandle, genericContext);
                default:
                    throw new ArgumentOutOfRangeException(nameof(typeHandle));
            }
        }

        internal MethodInfo ResolveMethodHandle(EntityHandle methodHandle, GenericContext genericContext)
        {
            if (methodHandle.IsNil)
            {
                throw new ArgumentNullException(nameof(methodHandle));
            }

            switch (methodHandle.Kind)
            {
                case HandleKind.MethodDefinition:
                    return ResolveMethodDefinition((MethodDefinitionHandle)methodHandle);
                case HandleKind.MemberReference:
                    return ResolveMethodReference((MemberReferenceHandle)methodHandle, genericContext);
                case HandleKind.MethodSpecification:
                    return ResolveMethodSpecification((MethodSpecificationHandle)methodHandle, genericContext);
                default:
                    throw new InvalidOperationException();
            }
        }

        internal FieldInfo ResolveFieldHandle(EntityHandle fieldHandle, GenericContext genericContext)
        {
            if (fieldHandle.IsNil)
            {
                throw new ArgumentNullException(nameof(fieldHandle));
            }

            switch (fieldHandle.Kind)
            {
                case HandleKind.FieldDefinition:
                    return ResolveFieldDefinition((FieldDefinitionHandle)fieldHandle);
                case HandleKind.MemberReference:
                    return ResolveFieldReference((MemberReferenceHandle)fieldHandle, genericContext);
                default:
                    throw new InvalidOperationException();
            }
        }

        #endregion

        #region Internal type handle resolution

        internal DefinedType ResolveTypeDefinition(TypeDefinitionHandle typeDefinitionHandle)
        {
            var resolvedDefinition = ResolveTypeDefinitionInternal(typeDefinitionHandle);
            var declaringType = resolvedDefinition.ResolveDeclaringType();

            return new DefinedType(ResolveTypeDefinitionInternal(typeDefinitionHandle), declaringType, null);
        }

        internal DefinedType ResolveTypeReference(TypeReferenceHandle typeReferenceHandle)
        {
            var typeReference = MetadataReader.GetTypeReference(typeReferenceHandle);

            var typeNamespace = MetadataReader.GetString(typeReference.Namespace);
            var typeName = MetadataReader.GetString(typeReference.Name);

            if (typeReference.ResolutionScope.IsNil)
            {
                return Assembly.ResolveExportedTypeTableType(typeNamespace, typeName);
            }
            else
            {
                var resolutionScope = typeReference.ResolutionScope;

                switch (resolutionScope.Kind)
                {
                    case HandleKind.ModuleDefinition:
                        return ResolveType(typeNamespace, typeName);
                    case HandleKind.ModuleReference:

                        var moduleReference = MetadataReader.GetModuleReference((ModuleReferenceHandle)resolutionScope);
                        var module = Assembly.ResolveModuleReference(moduleReference);

                        return module.ResolveType(typeNamespace, typeName);

                    case HandleKind.AssemblyReference:

                        var assemblyReference = MetadataReader.GetAssemblyReference((AssemblyReferenceHandle)resolutionScope);
                        var referencedAssembly = MetadataContext.ResolveAssemblyReference(MetadataReader, assemblyReference);

                        return referencedAssembly.ResolveType(typeNamespace, typeName);

                    case HandleKind.TypeReference:

                        var parentTypeReferenceHandle = (TypeReferenceHandle)resolutionScope;
                        var parentType = ResolveTypeReference(parentTypeReferenceHandle);

                        return parentType.ResolveNestedType(typeName);

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        internal TypeInfo ResolveTypeSpecification(
            TypeSpecificationHandle typeSpecificationHandle, GenericContext genericContext)
        {
            var typeSpecification = MetadataReader.GetTypeSpecification(typeSpecificationHandle);
            return typeSpecification.DecodeSignature(TypeProvider, genericContext);
        }

        #endregion

        #region Internal method handle resolution

        internal MethodInfo ResolveMethodDefinition(MethodDefinitionHandle methodDefinitionHandle)
        {
            var resolvedDefinition = ResolveMethodDefinitionInternal(methodDefinitionHandle);
            var declaringType = resolvedDefinition.ResolveDeclaringType();

            return new MethodInfo(resolvedDefinition, declaringType, null);
        }

        internal MethodInfo ResolveMethodReference(
            MemberReferenceHandle methodReferenceHandle,
            GenericContext genericContext)
        {
            var memberReference = MetadataReader.GetMemberReference(methodReferenceHandle);

            if (memberReference.GetKind() != MemberReferenceKind.Method)
            {
                throw new InvalidOperationException();
            }

            var name = MetadataReader.GetString(memberReference.Name);
            var parentHandle = memberReference.Parent;

            switch (parentHandle.Kind)
            {
                case HandleKind.TypeDefinition:
                case HandleKind.TypeReference:
                case HandleKind.TypeSpecification:

                    var declaringType = ResolveTypeHandle(parentHandle, genericContext);
                    var signature = memberReference.DecodeMethodSignature(
                        TypeProvider, declaringType.GenericContext ?? GenericContext.Empty);

                    if (declaringType is DefinedType typeDefinition)
                    {
                        return typeDefinition.ResolveMethod(name, signature);
                    }

                    throw new BadImageFormatException($"Invalid declaring type '{declaringType}' on member reference!");

                case HandleKind.ModuleReference:

                    var moduleReference = MetadataReader.GetModuleReference((ModuleReferenceHandle)parentHandle);
                    var module = Assembly.ResolveModuleReference(moduleReference);

                    signature = memberReference.DecodeMethodSignature(TypeProvider, GenericContext.Empty);

                    return module.ResolveGlobalMethod(name, signature);

                case HandleKind.MethodDefinition:
                    throw new NotImplementedException();
                default:
                    throw new BadImageFormatException();
            }
        }

        internal MethodInfo ResolveMethodSpecification(
            MethodSpecificationHandle methodSpecificationHandle,
            GenericContext genericContext)
        {
            var methodSpecification = MetadataReader.GetMethodSpecification(methodSpecificationHandle);
            var method = ResolveMethodHandle(methodSpecification.Method, genericContext);

            var methodArguments = methodSpecification.DecodeSignature(TypeProvider, genericContext);

            return method.MakeGenericMethod(methodArguments);
        }

        #endregion

        #region Internal field handle resolution

        internal FieldInfo ResolveFieldDefinition(FieldDefinitionHandle fieldDefinitionHandle)
        {
            var resolvedDefinition = ResolveFieldDefinitionInternal(fieldDefinitionHandle);
            var declaringType = resolvedDefinition.ResolveDeclaringType();

            return new FieldInfo(resolvedDefinition, declaringType);
        }

        internal FieldInfo ResolveFieldReference(
            MemberReferenceHandle fieldReferenceHandle,
            GenericContext genericContext)
        {
            var memberReference = MetadataReader.GetMemberReference(fieldReferenceHandle);

            if (memberReference.GetKind() != MemberReferenceKind.Field)
            {
                throw new InvalidOperationException();
            }

            var name = MetadataReader.GetString(memberReference.Name);
            var parentHandle = memberReference.Parent;

            switch (parentHandle.Kind)
            {
                case HandleKind.TypeDefinition:
                case HandleKind.TypeReference:
                case HandleKind.TypeSpecification:

                    var declaringType = ResolveTypeHandle(parentHandle, genericContext);
                    var signature = memberReference.DecodeFieldSignature(TypeProvider, declaringType.GenericContext);

                    if (declaringType is DefinedType typeDefinition)
                    {
                        return typeDefinition.ResolveField(name, signature);
                    }

                    throw new BadImageFormatException($"Invalid declaring type '{declaringType}' on member reference!");

                case HandleKind.ModuleReference:

                    var moduleReference = MetadataReader.GetModuleReference((ModuleReferenceHandle)parentHandle);
                    var module = Assembly.ResolveModuleReference(moduleReference);

                    signature = memberReference.DecodeFieldSignature(TypeProvider, GenericContext.Empty);

                    return module.ResolveGlobalField(name, signature);

                case HandleKind.MethodDefinition:
                    throw new NotImplementedException();
                default:
                    throw new InvalidOperationException();
            }
        }

        #endregion

        internal DefinedType ResolveType(string typeNamespace, string name)
        {
            foreach (var type in Types)
            {
                if (type.Namespace == typeNamespace
                    && type.Name == name)
                {
                    return type;
                }
            }

            throw new InvalidOperationException();
        }

        internal void Dispose() => _moduleReader.Dispose();

        private string GetName() => MetadataReader.GetString(_moduleDefinition.Name);

        private IEnumerable<DefinedType> ResolveTypes()
        {
            var types = new List<DefinedType>();

            foreach (var handle in MetadataReader.TypeDefinitions)
            {
                types.Add(ResolveTypeDefinition(handle));
            }

            return types;
        }

        private IEnumerable<MethodInfo> ResolveMethods()
        {
            var methods = new List<MethodInfo>();

            foreach (var handle in MetadataReader.MethodDefinitions)
            {
                var methodDefinition = MetadataReader.GetMethodDefinition(handle);

                if (methodDefinition.GetDeclaringType().IsNil)
                {
                    methods.Add(ResolveMethodDefinition(handle));
                }
            }

            return methods;
        }

        private IEnumerable<FieldInfo> ResolveFields()
        {
            var fields = new List<FieldInfo>();

            foreach (var handle in MetadataReader.FieldDefinitions)
            {
                var fieldDefinition = MetadataReader.GetFieldDefinition(handle);

                if (fieldDefinition.GetDeclaringType().IsNil)
                {
                    fields.Add(ResolveFieldDefinition(handle));
                }
            }

            return fields;
        }

        private IReadOnlyCollection<CustomAttributeInfo> ResolveCustomAttributes() =>
            this.ResolveCustomAttributes(_moduleDefinition.GetCustomAttributes());

        private MethodInfo ResolveGlobalMethod(string name, MethodSignature<TypeInfo> signature)
        {
            foreach (var method in Methods)
            {
                if (method.Matches(name, signature))
                {
                    return method;
                }
            }

            throw new InvalidOperationException();
        }

        private FieldInfo ResolveGlobalField(string name, TypeInfo signature)
        {
            foreach (var field in Fields)
            {
                if (field.Matches(name, signature))
                {
                    return field;
                }
            }

            throw new InvalidOperationException();
        }
    }
}
