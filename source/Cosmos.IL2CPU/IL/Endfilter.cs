using System;


namespace Cosmos.IL2CPU.X86.IL
{
  [OpCode(ILOpCode.Code.Endfilter)]
  public class Endfilter: ILOp
  {
    public Endfilter(XSharp.Assembler.Assembler aAsmblr):base(aAsmblr)
    {
    }

    public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode) {
      new Endfinally(Assembler).Execute(aMethod, aOpCode);
    }

  }
}
