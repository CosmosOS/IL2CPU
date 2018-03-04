using System;

namespace IL2CPU.API.Attribs { 
    [AttributeUsage(AttributeTargets.Assembly)]
    public class PlugAssembly : Attribute {
        public enum AssemblyType {
            Plugs,
            AssemblerPlugs
        }

        public PlugAssembly(AssemblyType aType) {
            Type = aType;
        }

        public AssemblyType Type { get; }
    }
}
