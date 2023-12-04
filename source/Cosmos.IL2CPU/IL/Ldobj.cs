using System;

using Cosmos.IL2CPU.ILOpCodes;

using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
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
      XS.Pop(RAX);
      var xObjSize = SizeOfType(type);

      if (xObjSize < 4 && TypeIsSigned(type))
      {
        if (xObjSize == 1)
        {
          XS.MoveSignExtend(RBX, RAX, sourceIsIndirect: true, size: RegisterSize.Byte8);
        }
        else if (xObjSize == 2)
        {
          XS.MoveSignExtend(RBX, RAX, sourceIsIndirect: true, size: RegisterSize.Short16);
        }
        XS.Push(RBX);
        return;
      }

      switch (xObjSize % 4)
      {
        case 1:
          {
            XS.Xor(RBX, RBX);
            XS.Set(BL, RAX, sourceDisplacement: (int)(xObjSize - 1));
            //XS.ShiftLeft(XSRegisters.EBX, 24);
            XS.Push(RBX);
            break;
          }
        case 2:
          {
            XS.Xor(RBX, RBX);
            XS.Set(BX, RAX, sourceDisplacement: (int)(xObjSize - 2));
            //XS.ShiftLeft(XSRegisters.EBX, 16);
            XS.Push(RBX);
            break;
          }
        case 3:
          {
            XS.Set(RBX, RAX, sourceDisplacement: (int)(xObjSize - 3));
            XS.And(RBX, 0xFFFFFF);
            XS.Push(RBX);
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
        XS.Push(RAX, displacement: (int)(xObjSize - i * 4));
      }
    }
  }
}
