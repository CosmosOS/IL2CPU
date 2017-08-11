using System;
using CPUx86 = XSharp.Assembler.x86;
using XSharp.Assembler;

using XSharp;

namespace Cosmos.IL2CPU.X86.IL
{
  [Cosmos.IL2CPU.OpCode(ILOpCode.Code.Stind_I)]
  public class Stind_I : ILOp
  {
    public Stind_I(XSharp.Assembler.Assembler aAsmblr)
        : base(aAsmblr)
    {
    }

    public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
    {
      Assemble(Assembler, 4, DebugEnabled);
    }

    public static void Assemble(XSharp.Assembler.Assembler aAssembler, int aSize, bool debugEnabled)
    {
      int xAlignedSize = (int)Align((uint)aSize, 4);

      XS.Comment($"address at: [esp+{xAlignedSize}]");
      DoNullReferenceCheck(aAssembler, debugEnabled, xAlignedSize);

      XS.Set(XSRegisters.EAX, XSRegisters.ESP, sourceIsIndirect: true, sourceDisplacement: xAlignedSize);
      for (int i = 0; i < xAlignedSize; i += 4)
      {
        XS.Pop(XSRegisters.EBX);
        XS.Set(XSRegisters.EAX, XSRegisters.EBX, destinationIsIndirect: true, destinationDisplacement: i);
      }
      XS.Add(XSRegisters.ESP, 4);
    }
  }
}
