using System;
using System.Reflection.Metadata;

using IL2CPU.Reflection.Internal;

namespace IL2CPU.Reflection.Types
{
    public class MethodPointerType : TypeInfo
    {
        public override ModuleInfo Module => null;

        public override string Namespace => null;
        public override string Name =>
            $"{_signature.Header.CallingConvention} {_signature.ReturnType} * ({String.Join(", ", _signature.ParameterTypes)})";

        public override TypeInfo DeclaringType => null;
        public override TypeInfo BaseType => null;

        public override bool IsClass => false;
        public override bool IsInterface => false;

        public override bool IsGenericType => false;

        private protected override TypeKind Kind => TypeKind.MethodPointer;

        private readonly MethodSignature<TypeInfo> _signature;

        internal MethodPointerType(MethodSignature<TypeInfo> signature)
        {
            _signature = signature;
        }

        public override int GetHashCode() =>
            _signature.ParameterTypes.Length * _signature.GenericParameterCount * _signature.ReturnType.GetHashCode();

        public override bool Equals(object obj) =>
            obj is MethodPointerType type
            && _signature.Matches(type._signature);
    }
}
