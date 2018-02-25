﻿using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using IL2CPU.Debug.Symbols.Metadata;

namespace IL2CPU.Debug.Symbols
{
    public class LocalTypeProvider : ISignatureTypeProvider<Type, LocalTypeGenericContext>
    {
        private Module mModule;

        public LocalTypeProvider(Module aModule)
        {
            mModule = aModule;
        }

        public Type GetPrimitiveType(PrimitiveTypeCode typeCode)
        {
            switch (typeCode)
            {
                case PrimitiveTypeCode.Void:
                    return typeof(void);
                case PrimitiveTypeCode.Boolean:
                    return typeof(bool);
                case PrimitiveTypeCode.Char:
                    return typeof(char);
                case PrimitiveTypeCode.SByte:
                    return typeof(sbyte);
                case PrimitiveTypeCode.Byte:
                    return typeof(byte);
                case PrimitiveTypeCode.Int16:
                    return typeof(short);
                case PrimitiveTypeCode.UInt16:
                    return typeof(ushort);
                case PrimitiveTypeCode.Int32:
                    return typeof(int);
                case PrimitiveTypeCode.UInt32:
                    return typeof(uint);
                case PrimitiveTypeCode.Int64:
                    return typeof(long);
                case PrimitiveTypeCode.UInt64:
                    return typeof(ulong);
                case PrimitiveTypeCode.Single:
                    return typeof(float);
                case PrimitiveTypeCode.Double:
                    return typeof(double);
                case PrimitiveTypeCode.String:
                    return typeof(string);
                case PrimitiveTypeCode.TypedReference:
                    return typeof(Type);
                case PrimitiveTypeCode.IntPtr:
                    return typeof(IntPtr);
                case PrimitiveTypeCode.UIntPtr:
                    return typeof(UIntPtr);
                case PrimitiveTypeCode.Object:
                    return typeof(object);
                default:
                    return null;
            }
        }

        public Type GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
        {
            return MetadataHelper.GetTypeFromReference(reader, mModule, handle, rawTypeKind);
        }

        public Type GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
        {
            return mModule.ResolveType(MetadataTokens.GetToken(handle), null, null);
        }

        public Type GetSZArrayType(Type elementType)
        {
            return elementType.MakeArrayType();
        }

        public Type GetGenericInstantiation(Type genericType, ImmutableArray<Type> typeArguments)
        {
            return genericType.MakeGenericType(typeArguments.ToArray());
        }

        public Type GetArrayType(Type elementType, ArrayShape shape)
        {
            return elementType.MakeArrayType();
        }

        public Type GetByReferenceType(Type elementType)
        {
            return elementType.MakeByRefType();
        }

        public Type GetPointerType(Type elementType)
        {
            return elementType.MakePointerType();
        }

        public Type GetFunctionPointerType(MethodSignature<Type> signature)
        {
            throw new NotImplementedException();
        }

        public Type GetPinnedType(Type elementType)
        {
            return elementType;
        }

        public Type GetTypeFromSpecification(MetadataReader reader, LocalTypeGenericContext genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
        {
            return reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
        }

        public Type GetModifiedType(Type modifier, Type unmodifiedType, bool isRequired)
        {
            throw new NotImplementedException();
        }

        public Type GetGenericTypeParameter(LocalTypeGenericContext genericContext, int index)
        {
            if (index > -1 && genericContext.TypeParameters.Length > index)
            {
                return genericContext.TypeParameters[index];
            }
            return null;
        }

        public Type GetGenericMethodParameter(LocalTypeGenericContext genericContext, int index)
        {
            if (index > -1 && genericContext.MethodParameters.Length > index)
            {
                return genericContext.MethodParameters[index];
            }
            return null;
        }
    }

}
