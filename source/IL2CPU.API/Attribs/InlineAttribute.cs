using System;

namespace IL2CPU.API.Attribs
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class InlineAttribute : Attribute
    {
        /// <summary>
        /// This field currently does nothing, but is here for later use.
        /// </summary>
        public TargetPlatform TargetPlatform = TargetPlatform.x86;
    }


}
