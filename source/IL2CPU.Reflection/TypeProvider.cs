using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;

using IL2CPU.Reflection.Internal;
using IL2CPU.Reflection.Types;
using DefinedType = IL2CPU.Reflection.Types.DefinedType;
using ReferencedDefinedType = IL2CPU.Reflection.Types.ReferencedDefinedType;

namespace IL2CPU.Reflection
{
    internal class TypeProvider : ISignatureTypeProvider<TypeInfo, GenericContext>, ICustomAttributeTypeProvider<TypeInfo>
    {
        private readonly MetadataContext _metadataContext;
        private readonly ModuleInfo _module;

        public TypeProvider(
            MetadataContext metadataContext,
            ModuleInfo module)
        {
            _metadataContext = metadataContext;
            _module = module;
        }

        public TypeInfo GetArrayType(TypeInfo elementType, ArrayShape shape) => new ArrayType(elementType, shape);
        public TypeInfo GetByReferenceType(TypeInfo elementType) => new ByReferenceType(elementType);
        public TypeInfo GetFunctionPointerType(MethodSignature<TypeInfo> signature) => new MethodPointerType(signature);

        public TypeInfo GetGenericInstantiation(TypeInfo genericType, ImmutableArray<TypeInfo> typeArguments) =>
            ((DefinedType)genericType).MakeGenericType(typeArguments);

        public TypeInfo GetGenericMethodParameter(GenericContext genericContext, int index) =>
            genericContext.GetMethodArgument(index);

        public TypeInfo GetGenericTypeParameter(GenericContext genericContext, int index) =>
            genericContext.GetTypeArgument(index);

        public TypeInfo GetModifiedType(TypeInfo modifier, TypeInfo unmodifiedType, bool isRequired) =>
            unmodifiedType.MakeModifiedType(modifier, isRequired);

        public TypeInfo GetPinnedType(TypeInfo elementType) => new PinnedType(elementType);
        public TypeInfo GetPointerType(TypeInfo elementType) => elementType.MakePointerType();
        public TypeInfo GetPrimitiveType(PrimitiveTypeCode typeCode) =>
            new BclTypeReference(_metadataContext, "System", typeCode.ToString());

        public TypeInfo GetSystemType() => _metadataContext.GetBclType(BclType.Type);

        public TypeInfo GetSZArrayType(TypeInfo elementType) => new SZArrayType(elementType);

        public TypeInfo GetTypeFromDefinition(
            MetadataReader reader,
            TypeDefinitionHandle handle,
            byte rawTypeKind) =>
            _module.ResolveTypeDefinition(handle);

        public TypeInfo GetTypeFromReference(
            MetadataReader reader,
            TypeReferenceHandle handle,
            byte rawTypeKind) =>
            new ReferencedDefinedType(_module, handle);

        public TypeInfo GetTypeFromSerializedName(string name) =>
            _metadataContext.ResolveTypeByNameInternal(name, assembly: _module.Assembly);

        public TypeInfo GetTypeFromSpecification(
            MetadataReader reader,
            GenericContext genericContext,
            TypeSpecificationHandle handle,
            byte rawTypeKind)
        {
            var typeSpecification = reader.GetTypeSpecification(handle);
            return typeSpecification.DecodeSignature(this, genericContext);
        }

        public PrimitiveTypeCode GetUnderlyingEnumType(TypeInfo type)
        {
            if (type is DefinedType typeDefinition)
            {
                var valueField = typeDefinition.Fields.Single(f => !f.IsStatic);
                return valueField.FieldType.GetPrimitiveTypeCode();
            }

            throw new NotImplementedException();
        }

        public bool IsSystemType(TypeInfo type) =>
            type is DefinedType typeDefinition && typeDefinition.Namespace == "System" && typeDefinition.Name == "Type";
    }
}
