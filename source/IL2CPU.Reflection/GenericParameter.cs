using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using Metadata = System.Reflection.Metadata;

namespace IL2CPU.Reflection
{
    public class GenericParameter
    {
        public string Name => _name.Value;

        public IEnumerable<GenericParameterConstraint> Constraints => _constraints.Value;

        public GenericParameterAttributes Attributes => _genericParameter.Attributes;

        #region Generic Parameter attribute wrappers

        public bool IsContravariant => Attributes.HasFlag(GenericParameterAttributes.Contravariant);
        public bool IsCovariant => Attributes.HasFlag(GenericParameterAttributes.Covariant);

        public bool RequiresDefaultConstructor => Attributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint);
        public bool RequiresNotNullableValueType => Attributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint);
        public bool RequiresReferenceType => Attributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint);

        #endregion

        internal int Index => _genericParameter.Index;

        private readonly ModuleInfo _module;

        private readonly GenericContext _genericContext;

        private readonly GenericParameterHandle _genericParameterHandle;
        private readonly Metadata.GenericParameter _genericParameter;

        private readonly Lazy<string> _name;

        private readonly Lazy<IEnumerable<GenericParameterConstraint>> _constraints;

        internal GenericParameter(
            ModuleInfo module,
            GenericParameterHandle genericParameterHandle,
            GenericContext genericContext)
        {
            _module = module;

            _genericContext = genericContext;

            _genericParameterHandle = genericParameterHandle;
            _genericParameter = _module.MetadataReader.GetGenericParameter(_genericParameterHandle);

            _name = new Lazy<string>(GetName);

            _constraints = new Lazy<IEnumerable<GenericParameterConstraint>>(ResolveConstraints);
        }

        private string GetName() => _module.MetadataReader.GetString(_genericParameter.Name);

        private IEnumerable<GenericParameterConstraint> ResolveConstraints()
        {
            var handles = _genericParameter.GetConstraints();
            var constraints = new List<GenericParameterConstraint>(handles.Count);

            foreach (var handle in handles)
            {
                constraints.Add(new GenericParameterConstraint(_module, handle, _genericContext));
            }

            return constraints;
        }
    }
}
