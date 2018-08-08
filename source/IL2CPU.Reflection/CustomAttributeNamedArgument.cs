using System.Reflection.Metadata;

namespace IL2CPU.Reflection
{
    public class CustomAttributeNamedArgument
    {
        public string Name => _namedArgument.Name;

        public TypeInfo Type => _namedArgument.Type;
        public object Value => _namedArgument.Value;

        private readonly CustomAttributeNamedArgument<TypeInfo> _namedArgument;

        internal CustomAttributeNamedArgument(
            CustomAttributeNamedArgument<TypeInfo> namedArgument)
        {
            _namedArgument = namedArgument;
        }
    }
}
