using XSharp;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
  [OpCode(ILOpCode.Code.Ldind_I1)]
  public class Ldind_I1 : ILOp
  {
    public Ldind_I1(XSharp.Assembler.Assembler aAsmblr)
        : base(aAsmblr)
    {
    }

    public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
    {
      DoNullReferenceCheck(Assembler, DebugEnabled, 0);
      XS.Pop(ECX);
      XS.MoveSignExtend(EAX, ECX, sourceIsIndirect: true, size: RegisterSize.Byte8);
      XS.Push(EAX);
    }
  }
}
