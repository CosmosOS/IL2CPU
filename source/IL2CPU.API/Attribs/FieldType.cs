using System;

namespace IL2CPU.API.Attribs
{
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
	public sealed class FieldType : Attribute {
        public string Name { get; set; }
    }
}