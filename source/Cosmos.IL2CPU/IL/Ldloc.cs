using Cosmos.IL2CPU.ILOpCodes;
using IL2CPU.Reflection;

using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;


namespace Cosmos.IL2CPU.X86.IL
{
  [OpCode(ILOpCode.Code.Ldloc)]
  public class Ldloc : ILOp
  {
    public Ldloc(Assembler aAsmblr)
      : base(aAsmblr)
    {
    }

    public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
    {
      var xOpVar = (OpVar)aOpCode;
      var xVar = aMethod.MethodInfo.MethodBody.LocalTypes[xOpVar.Value];

      if (xVar.IsPinned)
      {
        xVar = xVar.GetElementType();
      }

      var xStackCount = (int)GetStackCountForLocal(aMethod, xVar);
      var xEBPOffset = (int)GetEBPOffsetForLocal(aMethod, xOpVar.Value);
      var xSize = SizeOfType(xVar);
      bool xSigned = IsIntegerSigned(xVar);

      XS.Comment("Local type = " + xVar);
      XS.Comment("Local EBP offset = " + xEBPOffset);
      XS.Comment("Local size = " + xSize);

      switch (xSize)
      {
        case 1:
          if (xSigned)
          {
            XS.MoveSignExtend(EAX, EBP, sourceIsIndirect: true, sourceDisplacement: (0 - xEBPOffset), size: RegisterSize.Byte8);
          }
          else
          {
            XS.MoveZeroExtend(EAX, EBP, sourceIsIndirect: true, sourceDisplacement: (0 - xEBPOffset), size: RegisterSize.Byte8);
          }
          XS.Push(EAX);
          break;
        case 2:
          if (xSigned)
          {
            XS.MoveSignExtend(EAX, EBP, sourceIsIndirect: true, sourceDisplacement: (0 - xEBPOffset), size: RegisterSize.Short16);
          }
          else
          {
            XS.MoveZeroExtend(EAX, EBP, sourceIsIndirect: true, sourceDisplacement: (0 - xEBPOffset), size: RegisterSize.Short16);
          }
          XS.Push(EAX);
          break;
        default:
          for (int i = 0; i < xStackCount; i++)
          {
            XS.Set(EAX, EBP, sourceDisplacement: 0 - (xEBPOffset + (i * 4)));
            XS.Push(EAX);
          }
          break;
      }
    }
  }
}
