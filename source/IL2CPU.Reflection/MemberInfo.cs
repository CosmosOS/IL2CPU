using System.Collections.Generic;

namespace IL2CPU.Reflection
{
    public abstract class MemberInfo : ICustomAttributeProvider
    {
        public AssemblyInfo Assembly => Module?.Assembly;

        public abstract ModuleInfo Module { get; }

        public abstract int MetadataToken { get; }

        public abstract TypeInfo DeclaringType { get; }

        public abstract IReadOnlyCollection<CustomAttributeInfo> CustomAttributes { get; }
    }
}
