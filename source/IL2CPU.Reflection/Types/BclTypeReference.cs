using System;

using IL2CPU.Reflection.Internal;

namespace IL2CPU.Reflection.Types
{
    internal class BclTypeReference : DefinedType
    {
        public override string Namespace { get; }
        public override string Name { get; }

        internal override ResolvedTypeDefinition ResolvedDefinition => _type.Value.ResolvedDefinition;

        private readonly MetadataContext _metadataContext;

        private readonly Lazy<DefinedType> _type;

        public BclTypeReference(
            MetadataContext metadataContext,
            string typeNamespace,
            string typeName)
        {
            Namespace = typeNamespace;
            Name = typeName;

            _metadataContext = metadataContext;

            _type = new Lazy<DefinedType>(ResolveType);
        }

        private DefinedType ResolveType() =>
            _metadataContext.BclAssembly.ResolveExportedType(Namespace, Name);
    }
}
