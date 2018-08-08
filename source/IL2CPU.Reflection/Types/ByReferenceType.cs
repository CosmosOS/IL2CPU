using System.Collections.Generic;

namespace IL2CPU.Reflection.Types
{
    public class ByReferenceType : TypeInfo
    {
        public override ModuleInfo Module => ElementType.Module;

        public override string Namespace => ElementType.Namespace;
        public override string Name => $"{ElementType.Name}&";

        public override TypeInfo DeclaringType => ElementType.DeclaringType;
        public override TypeInfo BaseType => null;

        public TypeInfo ElementType { get; }

        public override bool IsClass => false;
        public override bool IsInterface => false;

        public override bool IsGenericType => false;

        private protected override TypeKind Kind => TypeKind.ByReference;

        internal ByReferenceType(TypeInfo elementType)
        {
            ElementType = elementType;
        }

        public override int GetHashCode() => EqualityComparer<TypeInfo>.Default.GetHashCode(ElementType);

        public override bool Equals(object obj) =>
            obj is ByReferenceType other
            && EqualityComparer<TypeInfo>.Default.Equals(ElementType, other.ElementType);
    }
}
