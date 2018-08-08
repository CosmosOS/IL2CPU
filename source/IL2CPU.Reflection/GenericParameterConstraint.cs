using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using Metadata = System.Reflection.Metadata;

using IL2CPU.Reflection.Internal;

namespace IL2CPU.Reflection
{
    public class GenericParameterConstraint
    {
        public TypeInfo Type => _type.Value;

        public IReadOnlyCollection<CustomAttributeInfo> CustomAttributes => _customAttributes.Value;

        private readonly ModuleInfo _module;

        private readonly GenericParameterConstraintHandle _genericParameterConstraintHandle;
        private readonly Metadata.GenericParameterConstraint _genericParameterConstraint;

        private readonly GenericContext _genericContext;

        private readonly Lazy<TypeInfo> _type;

        private readonly Lazy<IReadOnlyCollection<CustomAttributeInfo>> _customAttributes;

        internal GenericParameterConstraint(
            ModuleInfo module,
            GenericParameterConstraintHandle genericParameterConstraintHandle,
            GenericContext genericContext)
        {
            _module = module;

            _genericParameterConstraintHandle = genericParameterConstraintHandle;
            _genericParameterConstraint = _module.MetadataReader.GetGenericParameterConstraint(
                _genericParameterConstraintHandle);

            _genericContext = genericContext;

            _type = new Lazy<TypeInfo>(ResolveType);

            _customAttributes = new Lazy<IReadOnlyCollection<CustomAttributeInfo>>(ResolveCustomAttributes);
        }

        private TypeInfo ResolveType() => _module.ResolveTypeHandle(_genericParameterConstraint.Type, _genericContext);

        private IReadOnlyCollection<CustomAttributeInfo> ResolveCustomAttributes() =>
            _module.ResolveCustomAttributes(_genericParameterConstraint.GetCustomAttributes());
    }
}
