using System.Collections.Generic;

namespace IL2CPU.Reflection.Types
{
    public class SZArrayType : TypeInfo
    {
        public override ModuleInfo Module => ElementType.Module;

        public override string Namespace => ElementType.Namespace;
        public override string Name => $"{ElementType.Name}[]";

        public override TypeInfo DeclaringType => ElementType.DeclaringType;
        public override TypeInfo BaseType => new BclTypeReference(Module.MetadataContext, "System", "Array");

        public TypeInfo ElementType { get; }

        public override IReadOnlyCollection<TypeInfo> ExplicitImplementedInterfaces
        {
            get
            {
                if (_implementedInterfaces != null)
                {
                    return _implementedInterfaces;
                }

                return _implementedInterfaces = new List<TypeInfo>(5)
                {
                    Module.MetadataContext.GetBclType(BclType.IListOfT).MakeGenericType(ElementType),
                    Module.MetadataContext.GetBclType(BclType.ICollectionOfT).MakeGenericType(ElementType),
                    Module.MetadataContext.GetBclType(BclType.IEnumerableOfT).MakeGenericType(ElementType),
                    Module.MetadataContext.GetBclType(BclType.IReadOnlyListOfT).MakeGenericType(ElementType),
                    Module.MetadataContext.GetBclType(BclType.IReadOnlyCollectionOfT).MakeGenericType(ElementType)
                };
            }
        }

        public override bool IsClass => true;
        public override bool IsInterface => false;

        public override bool IsGenericType => false;

        private protected override TypeKind Kind => TypeKind.SZArray;

        private IReadOnlyCollection<TypeInfo> _implementedInterfaces;

        internal SZArrayType(TypeInfo elementType)
        {
            ElementType = elementType;
        }

        public override int GetHashCode() => EqualityComparer<TypeInfo>.Default.GetHashCode(ElementType);

        public override bool Equals(object obj) =>
            obj is SZArrayType other
            && EqualityComparer<TypeInfo>.Default.Equals(ElementType, other.ElementType);
    }
}
