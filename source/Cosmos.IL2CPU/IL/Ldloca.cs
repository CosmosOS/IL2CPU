using Cosmos.IL2CPU.Extensions;
using Cosmos.IL2CPU.ILOpCodes;
using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.IL
{
  [OpCode(ILOpCode.Code.Ldloca)]
  public class Ldloca : ILOp
  {
    public Ldloca(Assembler aAsmblr)
      : base(aAsmblr)
    {
    }

    public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
    {
      var xOpVar = (OpVar)aOpCode;
      var xVar = aMethod.MethodBase.GetLocalVariables()[xOpVar.Value];
      var xEBPOffset = GetEBPOffsetForLocal(aMethod, xOpVar.Value);
      xEBPOffset += (uint)(((int)GetStackCountForLocal(aMethod, xVar.LocalType) - 1) * 4);

      XS.Comment("Local type = " + xVar.LocalType);
      XS.Comment("Local EBP offset = " + xEBPOffset);

      XS.Set(EAX, EBP);
      XS.Sub(EAX, xEBPOffset);
      XS.Push(EAX);
    }
  }
}
