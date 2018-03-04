using System;
using System.Collections.Generic;
using System.Text;

namespace IL2CPU.API.Attribs
{
    public class ManifestResourceStreamAttribute : Attribute
    {
        public string ResourceName { get; set; }
    }
}
