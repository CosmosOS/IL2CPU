using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace IL2CPU.Reflection.Internal
{
    internal class ResolvedPropertyDefinition : ResolvedDefinitionBase
    {
        public override ModuleInfo Module { get; }

        public int MetadataToken => Module.MetadataReader.GetToken(_propertyDefinitionHandle);

        public string Name { get; }

        public ResolvedMethodDefinition GetMethod => _getMethod.Value;
        public ResolvedMethodDefinition SetMethod => _setMethod.Value;

        public IReadOnlyList<ResolvedMethodDefinition> OtherAccessorMethods => _otherAccessors.Value;

        public object DefaultValue => _defaultValue.Value;

        public PropertyAttributes Attributes => _propertyDefinition.Attributes;

        protected override CustomAttributeHandleCollection CustomAttributeHandles =>
            _propertyDefinition.GetCustomAttributes();

        private readonly PropertyDefinitionHandle _propertyDefinitionHandle;
        private readonly PropertyDefinition _propertyDefinition;

        private PropertyAccessors _propertyAccessors;


        private readonly Lazy<ResolvedMethodDefinition> _getMethod;
        private readonly Lazy<ResolvedMethodDefinition> _setMethod;

        private readonly Lazy<IReadOnlyList<ResolvedMethodDefinition>> _otherAccessors;

        private readonly Lazy<object> _defaultValue;

        public ResolvedPropertyDefinition(
            ModuleInfo module,
            PropertyDefinitionHandle propertyDefinitionHandle)
        {
            Module = module;

            _propertyDefinitionHandle = propertyDefinitionHandle;
            _propertyDefinition = Module.MetadataReader.GetPropertyDefinition(_propertyDefinitionHandle);

            _propertyAccessors = _propertyDefinition.GetAccessors();

            Name = Module.MetadataReader.GetString(_propertyDefinition.Name);

            _getMethod = new Lazy<ResolvedMethodDefinition>(ResolveGetMethod);
            _setMethod = new Lazy<ResolvedMethodDefinition>(ResolveSetMethod);

            _otherAccessors = new Lazy<IReadOnlyList<ResolvedMethodDefinition>>(ResolveOtherAccessors);

            _defaultValue = new Lazy<object>(ResolveDefaultValue);
        }

        internal MethodSignature<TypeInfo> DecodeSignature(GenericContext genericContext) =>
            _propertyDefinition.DecodeSignature(Module.TypeProvider, genericContext);

        internal ImmutableArray<byte> GetDefaultValueContent()
        {
            if (!Attributes.HasFlag(PropertyAttributes.HasDefault))
            {
                return ImmutableArray<byte>.Empty;
            }

            var defaultValue = Module.MetadataReader.GetConstant(_propertyDefinition.GetDefaultValue());
            return Module.MetadataReader.GetBlobContent(defaultValue.Value);
        }

        private ResolvedMethodDefinition ResolveGetMethod() => Module.ResolveMethodDefinitionInternal(_propertyAccessors.Getter);
        private ResolvedMethodDefinition ResolveSetMethod() => Module.ResolveMethodDefinitionInternal(_propertyAccessors.Setter);

        private IReadOnlyList<ResolvedMethodDefinition> ResolveOtherAccessors()
        {
            var accessors = new List<ResolvedMethodDefinition>(_propertyAccessors.Others.Length);

            foreach (var accessor in _propertyAccessors.Others)
            {
                accessors.Add(Module.ResolveMethodDefinitionInternal(accessor));
            }

            return accessors;
        }

        private object ResolveDefaultValue()
        {
            if (!Attributes.HasFlag(FieldAttributes.HasDefault))
            {
                return null;
            }

            var defaultValue = Module.MetadataReader.GetConstant(_propertyDefinition.GetDefaultValue());
            return defaultValue.GetConstantValue(Module);
        }
    }
}
