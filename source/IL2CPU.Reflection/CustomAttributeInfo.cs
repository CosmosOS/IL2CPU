using System;
using System.Collections.Generic;
using System.Reflection.Metadata;

namespace IL2CPU.Reflection
{
    public class CustomAttributeInfo
    {
        public MethodInfo Constructor => _constructor.Value;
        public TypeInfo AttributeType => Constructor.DeclaringType;

        public IReadOnlyList<CustomAttributeFixedArgument> FixedArguments => _fixedArguments.Value;

        public IReadOnlyList<CustomAttributeNamedArgument> FieldArguments => _namedArguments.Value.FieldArguments;
        public IReadOnlyList<CustomAttributeNamedArgument> PropertyArguments => _namedArguments.Value.PropertyArguments;

        private readonly CustomAttribute _customAttribute;
        private readonly ModuleInfo _module;

        private readonly Lazy<CustomAttributeValue<TypeInfo>> _customAttributeValue;

        private readonly Lazy<MethodInfo> _constructor;

        private readonly Lazy<IReadOnlyList<CustomAttributeFixedArgument>> _fixedArguments;
        private readonly Lazy<NamedArguments> _namedArguments;

        internal CustomAttributeInfo(
            ModuleInfo module,
            CustomAttribute customAttribute)
        {
            _customAttribute = customAttribute;
            _module = module;

            _customAttributeValue = new Lazy<CustomAttributeValue<TypeInfo>>(GetValue);

            _constructor = new Lazy<MethodInfo>(ResolveConstructor);

            _fixedArguments = new Lazy<IReadOnlyList<CustomAttributeFixedArgument>>(ResolveFixedArguments);
            _namedArguments = new Lazy<NamedArguments>(ResolveNamedArguments);
        }

        private CustomAttributeValue<TypeInfo> GetValue() => _customAttribute.DecodeValue(_module.TypeProvider);

        private MethodInfo ResolveConstructor() =>
            _module.ResolveMethodHandle(_customAttribute.Constructor, GenericContext.Empty);

        private IReadOnlyList<CustomAttributeFixedArgument> ResolveFixedArguments()
        {
            var fixedArguments = new List<CustomAttributeFixedArgument>();

            foreach (var fixedArgument in _customAttributeValue.Value.FixedArguments)
            {
                fixedArguments.Add(new CustomAttributeFixedArgument(fixedArgument));
            }

            return fixedArguments;
        }

        private NamedArguments ResolveNamedArguments()
        {
            var fieldArguments = new List<CustomAttributeNamedArgument>();
            var propertyArguments = new List<CustomAttributeNamedArgument>();

            foreach (var namedArgument in _customAttributeValue.Value.NamedArguments)
            {
                var argument = new CustomAttributeNamedArgument(namedArgument);

                switch (namedArgument.Kind)
                {
                    case CustomAttributeNamedArgumentKind.Field:
                        fieldArguments.Add(argument);
                        break;
                    case CustomAttributeNamedArgumentKind.Property:
                        propertyArguments.Add(argument);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            return new NamedArguments(fieldArguments, propertyArguments);
        }

        private class NamedArguments
        {
            public IReadOnlyList<CustomAttributeNamedArgument> FieldArguments { get; }
            public IReadOnlyList<CustomAttributeNamedArgument> PropertyArguments { get; }

            public NamedArguments(
                IReadOnlyList<CustomAttributeNamedArgument> fieldArguments,
                IReadOnlyList<CustomAttributeNamedArgument> propertyArguments)
            {
                FieldArguments = fieldArguments;
                PropertyArguments = propertyArguments;
            }
        }
    }
}
