using System;

namespace IL2CPU.API.Attribs
{
    [AttributeUsage(System.AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class NoGC : Attribute
    {
        public NoGC()
        {
        }
    }
}
