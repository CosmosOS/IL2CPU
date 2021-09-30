using System;

namespace IL2CPU.Reflection
{
    public class SomeGenerics
    {
        public Type[] TypeParameters { get; internal set; }
        public Type[] MethodParameters { get; internal set; }
    }
}
