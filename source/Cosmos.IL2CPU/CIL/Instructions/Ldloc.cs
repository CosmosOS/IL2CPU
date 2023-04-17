using Cosmos.IL2CPU.CIL.ILOpCodes;
using Cosmos.IL2CPU.Extensions;
using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;


namespace Cosmos.IL2CPU.CIL.Instructions
{
  [OpCode(ILOpCode.Code.Ldloc)]
  public class Ldloc : ILOp
  {
    public Ldloc(Assembler aAsmblr)
      : base(aAsmblr)
    {
    }

    public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
    {
      var xOpVar = (OpVar)aOpCode;
      var xVar = aMethod.MethodBase.GetLocalVariables()[xOpVar.Value];
      var xStackCount = (int)GetStackCountForLocal(aMethod, xVar.LocalType);
      var xEBPOffset = (int)GetEBPOffsetForLocal(aMethod, xOpVar.Value);
      var xSize = SizeOfType(xVar.LocalType);
      bool xSigned = TypeIsSigned(xVar.LocalType);

      XS.Comment("Local type = " + xVar.LocalType);
      XS.Comment("Local EBP offset = " + xEBPOffset);
      XS.Comment("Local size = " + xSize);

      switch (xSize)
      {
        case 1:
          if (xSigned)
          {
            XS.MoveSignExtend(EAX, EBP, sourceIsIndirect: true, sourceDisplacement: 0 - xEBPOffset, size: RegisterSize.Byte8);
          }
          else
          {
            XS.MoveZeroExtend(EAX, EBP, sourceIsIndirect: true, sourceDisplacement: 0 - xEBPOffset, size: RegisterSize.Byte8);
          }
          XS.Push(EAX);
          break;
        case 2:
          if (xSigned)
          {
            XS.MoveSignExtend(EAX, EBP, sourceIsIndirect: true, sourceDisplacement: 0 - xEBPOffset, size: RegisterSize.Short16);
          }
          else
          {
            XS.MoveZeroExtend(EAX, EBP, sourceIsIndirect: true, sourceDisplacement: 0 - xEBPOffset, size: RegisterSize.Short16);
          }
          XS.Push(EAX);
          break;
        default:
          for (int i = 0; i < xStackCount; i++)
          {
            //XS.Set(EAX, EBP, sourceDisplacement: 0 - (xEBPOffset + (i * 4)));
            XS.Push(EBP, displacement: 0 - (xEBPOffset + i * 4));
          }
          break;
      }
    }
  }
}
