using System;

namespace IL2CPU.API.Attribs
{
    public class ManifestResourceStreamAttribute : Attribute
    {
        public string ResourceName { get; set; }
    }
}
