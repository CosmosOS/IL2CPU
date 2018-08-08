using System;
using System.Reflection.Metadata;

using IL2CPU.Reflection.Internal;

namespace IL2CPU.Reflection.Types
{
    internal class ReferencedDefinedType : DefinedType
    {
        public override ModuleInfo Module => ResolvedDefinition.Module;

        public override string Namespace => _namespace.Value;
        public override string Name => _name.Value;

        public override TypeInfo DeclaringType => _resolvedType.Value.DeclaringType;
        public override TypeInfo BaseType => _resolvedType.Value.BaseType;

        internal override ResolvedTypeDefinition ResolvedDefinition => _resolvedType.Value.ResolvedDefinition;

        private readonly ModuleInfo _referenceModule;

        private readonly TypeReferenceHandle _typeReferenceHandle;
        private readonly TypeReference _typeReference;

        private readonly Lazy<string> _namespace;
        private readonly Lazy<string> _name;

        private readonly Lazy<DefinedType> _resolvedType;

        public ReferencedDefinedType(
            ModuleInfo module,
            TypeReferenceHandle typeReferenceHandle)
        {
            _referenceModule = module;

            _typeReferenceHandle = typeReferenceHandle;
            _typeReference = _referenceModule.MetadataReader.GetTypeReference(_typeReferenceHandle);

            _namespace = new Lazy<string>(GetNamespace);
            _name = new Lazy<string>(GetName);

            _resolvedType = new Lazy<DefinedType>(ResolveType);
        }

        private string GetNamespace()
        {
            var typeNamespace = _referenceModule.MetadataReader.GetString(_typeReference.Namespace);

            if (String.IsNullOrEmpty(typeNamespace))
            {
                return null;
            }

            return typeNamespace;
        }

        private string GetName() => _referenceModule.MetadataReader.GetString(_typeReference.Name);

        private DefinedType ResolveType() => _referenceModule.ResolveTypeReference(_typeReferenceHandle);
    }
}
