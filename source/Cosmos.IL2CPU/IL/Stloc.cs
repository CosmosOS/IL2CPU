using Cosmos.IL2CPU.Extensions;
using Cosmos.IL2CPU.ILOpCodes;

using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
  [OpCode(ILOpCode.Code.Stloc)]
  public class Stloc : ILOp
  {
    public Stloc(Assembler aAsmblr)
      : base(aAsmblr)
    {
    }

    public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
    {
      var xOpVar = (OpVar) aOpCode;
      var xVar = aMethod.MethodBase.GetLocalVariables()[xOpVar.Value];
      var xStackCount = (int) GetStackCountForLocal(aMethod, xVar.Type);
      var xEBPOffset = (int) GetEBPOffsetForLocal(aMethod, xOpVar.Value);
      var xSize = SizeOfType(xVar.Type);

      XS.Comment("Local type = " + xVar.Type);
      XS.Comment("Local EBP offset = " + xEBPOffset);
      XS.Comment("Local size = " + xSize);

      for (int i = xStackCount - 1; i >= 0; i--)
      {
        XS.Pop(EAX);
        XS.Set(EBP, EAX, destinationDisplacement: 0 - (xEBPOffset + (i*4)));
      }
    }
  }
}
