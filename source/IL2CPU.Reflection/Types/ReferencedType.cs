using System;

using IL2CPU.Reflection.Internal;

namespace IL2CPU.Reflection.Types
{
    internal class ReferencedType : DefinedType
    {
        public override string Namespace { get; }
        public override string Name { get; }

        internal override ResolvedTypeDefinition ResolvedDefinition => _resolvedType.Value.ResolvedDefinition;

        private readonly Lazy<DefinedType> _resolvedType;

        public ReferencedType(
            string typeNamespace,
            string name,
            Func<DefinedType> resolver)
        {
            Namespace = typeNamespace;
            Name = name;

            _resolvedType = new Lazy<DefinedType>(resolver);
        }

        public override bool Equals(object obj) => obj is DefinedType definedType && definedType.Equals(_resolvedType.Value);
        public override int GetHashCode() => FullName.GetHashCode();
    }
}
