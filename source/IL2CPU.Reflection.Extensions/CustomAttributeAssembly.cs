using System.Linq;
using System.Reflection;

namespace IL2CPU.Reflection
{
    internal class CustomAttributeAssembly : Assembly
    {
        private readonly AssemblyIdentity _identity;

        public CustomAttributeAssembly(TypeInfo type)
        {
            _identity = type.Module.Assembly.Identity;
        }

        public override AssemblyName GetName()
        {
            var assemblyName = new AssemblyName()
            {
                Name = _identity.Name,
                CultureName = _identity.Culture,
                Version = _identity.Version,
                KeyPair = new StrongNameKeyPair(_identity.PublicKeyToken.ToArray())
            };

            return assemblyName;
        }
    }
}
