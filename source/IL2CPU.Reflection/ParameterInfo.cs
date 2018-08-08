using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;

using IL2CPU.Reflection.Internal;

namespace IL2CPU.Reflection
{
    public sealed class ParameterInfo : ICustomAttributeProvider
    {
        public string Name => _name.Value;

        public object DefaultValue => _defaultValue.Value;

        public IReadOnlyCollection<CustomAttributeInfo> CustomAttributes => _customAttributes.Value;

        public int Position => SequenceNumber - 1;

        public ParameterAttributes Attributes => _parameter.Attributes;

        #region Parameter attribute wrappers

        public bool IsIn => Attributes.HasFlag(ParameterAttributes.In);
        public bool IsOptional => Attributes.HasFlag(ParameterAttributes.Optional);
        public bool IsOut => Attributes.HasFlag(ParameterAttributes.Out);
        public bool IsRetval => Attributes.HasFlag(ParameterAttributes.Retval);

        #endregion

        internal int SequenceNumber => _parameter.SequenceNumber;

        ModuleInfo ICustomAttributeProvider.Module => _module;

        private readonly ModuleInfo _module;

        private readonly ParameterHandle _parameterHandle;
        private readonly Parameter _parameter;

        private readonly Lazy<string> _name;

        private readonly Lazy<object> _defaultValue;

        private Lazy<IReadOnlyCollection<CustomAttributeInfo>> _customAttributes;

        internal ParameterInfo(
            ModuleInfo module,
            ParameterHandle parameterHandle)
        {
            _module = module;

            _parameterHandle = parameterHandle;
            _parameter = _module.MetadataReader.GetParameter(_parameterHandle);

            _name = new Lazy<string>(GetName);

            _defaultValue = new Lazy<object>(ResolveDefaultValue);

            _customAttributes = new Lazy<IReadOnlyCollection<CustomAttributeInfo>>(ResolveCustomAttributes);
        }

        private string GetName() => _module.MetadataReader.GetString(_parameter.Name);

        private object ResolveDefaultValue()
        {
            if (!Attributes.HasFlag(ParameterAttributes.HasDefault))
            {
                return null;
            }

            var defaultValue = _module.MetadataReader.GetConstant(_parameter.GetDefaultValue());
            return defaultValue.GetConstantValue(_module);
        }
        private IReadOnlyCollection<CustomAttributeInfo> ResolveCustomAttributes() =>
            _module.ResolveCustomAttributes(_parameter.GetCustomAttributes());
    }
}
