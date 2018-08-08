using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;

namespace IL2CPU.Reflection.Types
{
    public class ArrayType : TypeInfo
    {
        public override ModuleInfo Module => ElementType.Module;

        public override string Namespace => ElementType.Namespace;
        public override string Name => $"{ElementType.Name}[{new string(',', Rank - 1)}]";

        public override TypeInfo DeclaringType => ElementType.DeclaringType;
        public override TypeInfo BaseType => new BclTypeReference(Module.MetadataContext, "System", "Array");

        public TypeInfo ElementType { get; }

        public int Rank => _arrayShape.Rank;

        public IReadOnlyList<int> LowerBounds => _arrayShape.LowerBounds;
        public IReadOnlyList<int> Sizes => _arrayShape.Sizes;

        public override bool IsClass => true;
        public override bool IsInterface => false;

        public override bool IsGenericType => false;

        private protected override TypeKind Kind => TypeKind.Array;
        
        private readonly ArrayShape _arrayShape;

        internal ArrayType(TypeInfo elementType, ArrayShape arrayShape)
        {
            ElementType = elementType;

            _arrayShape = arrayShape;
        }

        public override int GetHashCode() => EqualityComparer<TypeInfo>.Default.GetHashCode(ElementType);

        public override bool Equals(object obj) =>
            obj is ArrayType other
            && EqualityComparer<TypeInfo>.Default.Equals(ElementType, other.ElementType)
            && ArrayShapesAreEqual(_arrayShape, other._arrayShape);

        private static bool ArrayShapesAreEqual(ArrayShape shape, ArrayShape other) =>
            shape.Rank == other.Rank
            && shape.LowerBounds.SequenceEqual(other.LowerBounds)
            && shape.Sizes.SequenceEqual(other.Sizes);
    }
}
