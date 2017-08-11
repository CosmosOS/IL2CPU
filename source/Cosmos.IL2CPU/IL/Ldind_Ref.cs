using System;
using XSharp.Assembler;
using CPUx86 = XSharp.Assembler.x86;
using static XSharp.Common.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
  [OpCode(ILOpCode.Code.Ldind_Ref)]
  public class Ldind_Ref : ILOp
  {
    public Ldind_Ref(Assembler aAsmblr)
        : base(aAsmblr)
    {
    }

    public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
    {
      Ldind_I.Assemble(Assembler, 8, DebugEnabled);
    }
  }
}
