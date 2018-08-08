using System.Reflection;

namespace IL2CPU.Reflection
{
    internal class CustomAttributeModule : Module
    {
        public override Assembly Assembly { get; }

        public override string Name { get; }

        public CustomAttributeModule(TypeInfo type)
        {
            Assembly = new CustomAttributeAssembly(type);
            Name = type.Module.Name;
        }
    }
}
