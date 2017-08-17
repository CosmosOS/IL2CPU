using System;

namespace Cosmos.IL2CPU.API.Attribs {
  [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
  public sealed class PlugMethod: Attribute {
    public string Signature = null;
    public bool Enabled = true;
    public Type Assembler = null;
    public bool PlugRequired = false;
    public bool IsWildcard = false;
    public bool WildcardMatchParameters = false;
    public bool IsOptional = true;
  }
}
