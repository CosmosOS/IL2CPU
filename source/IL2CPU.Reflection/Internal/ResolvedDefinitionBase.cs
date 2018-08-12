using System;
using System.Collections.Generic;
using System.Reflection.Metadata;

namespace IL2CPU.Reflection.Internal
{
    internal abstract class ResolvedDefinitionBase
    {
        public abstract ModuleInfo Module { get; }

        public IReadOnlyCollection<CustomAttributeInfo> CustomAttributes => _customAttributes.Value;

        protected abstract CustomAttributeHandleCollection CustomAttributeHandles { get; }

        private Lazy<IReadOnlyCollection<CustomAttributeInfo>> _customAttributes;

        protected ResolvedDefinitionBase()
        {
            _customAttributes = new Lazy<IReadOnlyCollection<CustomAttributeInfo>>(ResolveCustomAttributes);
        }

        public TypeInfo ResolveDeclaringType()
        {
            var handle = GetDeclaringTypeHandle();

            if (handle.IsNil)
            {
                return null;
            }

            var resolvedDefinition = Module.ResolveTypeDefinitionInternal(handle);
            return new Types.DefinedType(resolvedDefinition, resolvedDefinition.ResolveDeclaringType(), null);
        }

        protected virtual TypeDefinitionHandle GetDeclaringTypeHandle() => default;

        private IReadOnlyCollection<CustomAttributeInfo> ResolveCustomAttributes() =>
            Module.ResolveCustomAttributes(CustomAttributeHandles);
    }
}
