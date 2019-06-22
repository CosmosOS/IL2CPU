using System;
using System.Reflection.Metadata;

namespace IL2CPU.Reflection.Internal
{
    internal static class TypeInfoExtensions
    {
        private const string SystemNamespace = nameof(System);

        private const string EnumType = nameof(Enum);
        private const string ValueTypeType = nameof(ValueType);

        public static PrimitiveTypeCode GetPrimitiveTypeCode(
            this TypeInfo type)
        {
            if (type.Namespace == SystemNamespace
                && Enum.TryParse<PrimitiveTypeCode>(type.Name, out var primitiveTypeCode))
            {
                return primitiveTypeCode;
            }

            throw new InvalidOperationException("Internal error");
        }

        public static bool IsEnum(this TypeInfo type) => type.IsBclType(BclType.Enum);

        public static bool IsValueType(this TypeInfo type) => type.IsBclType(BclType.ValueType);

        public static bool IsBclType(this TypeInfo type, BclType bclType) =>
            type.IsBclType(bclType.Namespace, bclType.Name);

        public static bool IsBclType(
            this TypeInfo type,
            string typeNamespace,
            string typeName) => type.Namespace == typeNamespace && type.Name == typeName;

    }
}
