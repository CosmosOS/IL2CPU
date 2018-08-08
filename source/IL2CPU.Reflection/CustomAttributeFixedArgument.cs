using System.Reflection.Metadata;

namespace IL2CPU.Reflection
{
    public class CustomAttributeFixedArgument
    {
        public TypeInfo Type => _fixedArgument.Type;
        public object Value => _fixedArgument.Value;

        private readonly CustomAttributeTypedArgument<TypeInfo> _fixedArgument;

        internal CustomAttributeFixedArgument(
            CustomAttributeTypedArgument<TypeInfo> fixedArgument)
        {
            _fixedArgument = fixedArgument;
        }
    }
}
