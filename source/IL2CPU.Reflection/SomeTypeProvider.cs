using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

namespace IL2CPU.Reflection
{
    public class SomeTypeProvider : ISignatureTypeProvider<Type, SomeGenerics>
    {
        private readonly Module _module;
        private readonly IBaseTypeSystem _types;

        public SomeTypeProvider(Module module, IBaseTypeSystem types)
        {
            _module = module;
            _types = types;
        }

        public Type GetArrayType(Type elementType, ArrayShape shape)
        {
            var refType = elementType.MakeArrayType();
            return refType;
        }

        public Type GetByReferenceType(Type elementType)
        {
            var refType = elementType.MakeByRefType();
            return refType;
        }

        public Type GetFunctionPointerType(MethodSignature<Type> signature)
        {
            throw new NotImplementedException();
        }

        public Type GetGenericInstantiation(Type genericType, ImmutableArray<Type> typeArguments)
        {
            var @params = typeArguments.ToArray();
            var genType = genericType.MakeGenericType(@params);
            return genType;
        }

        public Type GetGenericMethodParameter(SomeGenerics genericContext, int index)
        {
            if (index > -1 && genericContext.MethodParameters?.Length > index)
            {
                var par = genericContext.MethodParameters[index];
                return par;
            }
            return _types.FakeGenericArg(index);
        }

        public Type GetGenericTypeParameter(SomeGenerics genericContext, int index)
        {
            if (index > -1 && genericContext.TypeParameters?.Length > index)
            {
                var par = genericContext.TypeParameters[index];
                return par;
            }
            return _types.FakeGenericArg(index);
        }

        public Type GetModifiedType(Type modifier, Type unmodifiedType, bool isRequired)
        {
            var mod = modifier.FullName;
            switch (mod)
            {
                case "System.Runtime.CompilerServices.IsVolatile":
                    return unmodifiedType;
                case "System.Runtime.InteropServices.InAttribute":
                    return unmodifiedType;
                default:
                    throw new NotImplementedException(mod);
            }
        }

        public Type GetPinnedType(Type elementType)
        {
            return elementType;
        }

        public Type GetPointerType(Type elementType)
        {
            var pointerType = elementType.MakePointerType();
            return pointerType;
        }

        public Type GetPrimitiveType(PrimitiveTypeCode typeCode)
        {
            switch (typeCode)
            {
                case PrimitiveTypeCode.Boolean:
                    return _types.Boolean;
                case PrimitiveTypeCode.Byte:
                    return _types.Byte;
                case PrimitiveTypeCode.SByte:
                    return _types.SByte;
                case PrimitiveTypeCode.Char:
                    return _types.Char;
                case PrimitiveTypeCode.Int16:
                    return _types.Int16;
                case PrimitiveTypeCode.UInt16:
                    return _types.UInt16;
                case PrimitiveTypeCode.Int32:
                    return _types.Int32;
                case PrimitiveTypeCode.UInt32:
                    return _types.UInt32;
                case PrimitiveTypeCode.Int64:
                    return _types.Int64;
                case PrimitiveTypeCode.UInt64:
                    return _types.UInt64;
                case PrimitiveTypeCode.Single:
                    return _types.Single;
                case PrimitiveTypeCode.Double:
                    return _types.Double;
                case PrimitiveTypeCode.IntPtr:
                    return _types.IntPtr;
                case PrimitiveTypeCode.UIntPtr:
                    return _types.UIntPtr;
                case PrimitiveTypeCode.Object:
                    return _types.Object;
                case PrimitiveTypeCode.String:
                    return _types.String;
                case PrimitiveTypeCode.TypedReference:
                    return _types.TypedReference;
                case PrimitiveTypeCode.Void:
                    return _types.Void;
                default:
                    throw new ArgumentOutOfRangeException(nameof(typeCode), typeCode, null);
            }
        }

        public Type GetSZArrayType(Type elementType)
        {
            var arrayType = elementType.MakeArrayType();
            return arrayType;
        }

        public Type GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
        {
            var token = MetadataTokens.GetToken(handle);
            var type = _module.RetrieveType(token);
            return type;
        }

        public Type GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
        {
            var token = MetadataTokens.GetToken(handle);
            var type = _module.RetrieveType(token);
            return type;
        }

        public Type GetTypeFromSpecification(MetadataReader reader, SomeGenerics ctx,
            TypeSpecificationHandle handle, byte rawTypeKind)
        {
            var token = MetadataTokens.GetToken(handle);
            var type = _module.RetrieveType(token, ctx.TypeParameters, ctx.MethodParameters);
            return type;
        }
    }
}
