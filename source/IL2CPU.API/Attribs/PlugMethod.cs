using System;

namespace IL2CPU.API.Attribs {
  [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
  public sealed class PlugMethod: Attribute {
    public string Signature { get; set; }
    public bool Enabled { get; set; } = true;
    public Type Assembler { get; set; }
    public bool PlugRequired { get; set; }
    public bool IsWildcard { get; set; }
    public bool WildcardMatchParameters { get; set; }
    public bool IsOptional { get; set; } = true;
  }
}
