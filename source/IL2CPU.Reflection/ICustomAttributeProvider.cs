using System.Collections.Generic;

namespace IL2CPU.Reflection
{
    public interface ICustomAttributeProvider
    {
        ModuleInfo Module { get; }

        IReadOnlyCollection<CustomAttributeInfo> CustomAttributes { get; }
    }
}
