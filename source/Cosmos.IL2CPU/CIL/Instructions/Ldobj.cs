using System;
using Cosmos.IL2CPU.CIL.ILOpCodes;
using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.CIL.Instructions
{
  [OpCode(ILOpCode.Code.Ldobj)]
  public class Ldobj : ILOp
  {
    public Ldobj(Assembler aAsmblr)
        : base(aAsmblr)
    {
    }

    public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
    {
      DoNullReferenceCheck(Assembler, DebugEnabled, 0);
      OpType xType = (OpType)aOpCode;
      DoAssemble(xType.Value);
    }

    public static void DoAssemble(Type type)
    {
      if (type == null)
      {
        throw new ArgumentNullException(nameof(type));
      }
      XS.Pop(EAX);
      var xObjSize = SizeOfType(type);

      if (xObjSize < 4 && TypeIsSigned(type))
      {
        if (xObjSize == 1)
        {
          XS.MoveSignExtend(EBX, EAX, sourceIsIndirect: true, size: RegisterSize.Byte8);
        }
        else if (xObjSize == 2)
        {
          XS.MoveSignExtend(EBX, EAX, sourceIsIndirect: true, size: RegisterSize.Short16);
        }
        XS.Push(EBX);
        return;
      }

      switch (xObjSize % 4)
      {
        case 1:
          {
            XS.Xor(EBX, EBX);
            XS.Set(BL, EAX, sourceDisplacement: (int)(xObjSize - 1));
            //XS.ShiftLeft(XSRegisters.EBX, 24);
            XS.Push(EBX);
            break;
          }
        case 2:
          {
            XS.Xor(EBX, EBX);
            XS.Set(BX, EAX, sourceDisplacement: (int)(xObjSize - 2));
            //XS.ShiftLeft(XSRegisters.EBX, 16);
            XS.Push(EBX);
            break;
          }
        case 3:
          {
            XS.Set(EBX, EAX, sourceDisplacement: (int)(xObjSize - 3));
            XS.And(EBX, 0xFFFFFF);
            XS.Push(EBX);
            break;
          }
        case 0:
          {
            break;
          }
        default:
          throw new Exception("Remainder not supported!");
      }

      xObjSize -= xObjSize % 4;

      for (int i = 1; i <= xObjSize / 4; i++)
      {
        XS.Push(EAX, displacement: (int)(xObjSize - i * 4));
      }
    }
  }
}
