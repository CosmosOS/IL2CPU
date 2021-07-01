using System;

using XSharp;
using static XSharp.XSRegisters;
using CPUx86 = XSharp.Assembler.x86;
namespace Cosmos.IL2CPU.X86.IL
{
  [Cosmos.IL2CPU.OpCode(ILOpCode.Code.Initobj)]
  public class Initobj : ILOp
  {
    public Initobj(XSharp.Assembler.Assembler aAsmblr) : base(aAsmblr)
    {
    }

    public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
    {
      // MtW: for now, disable this instruction. To me, it's unclear in what context it's being used.
      uint mObjSize = 0;

      Type mType = ((Cosmos.IL2CPU.ILOpCodes.OpType)aOpCode).Value;
      mObjSize = SizeOfType(mType);

      XS.Pop(XSRegisters.EAX);
      for (int i = 0; i < (mObjSize / 4); i++)
      {
        XS.Set(EAX, 0, destinationDisplacement: i * 4, size: RegisterSize.Int32);
      }
      switch (mObjSize % 4)
      {
        case 1:
          {
            new CPUx86.Mov { DestinationReg = CPUx86.RegistersEnum.EAX, DestinationIsIndirect = true, DestinationDisplacement = (int)((mObjSize / 4) * 4), SourceValue = 0, Size = 8 };
            break;
          }
        case 2:
          {
            new CPUx86.Mov { DestinationReg = CPUx86.RegistersEnum.EAX, DestinationIsIndirect = true, DestinationDisplacement = (int)((mObjSize / 4) * 4), SourceValue = 0, Size = 16 };
            break;
          }
        case 3:
          {
            new CPUx86.Mov { DestinationReg = CPUx86.RegistersEnum.EAX, DestinationIsIndirect = true, DestinationDisplacement = (int)((mObjSize / 4) * 4), SourceValue = 0, Size = 8 };
            new CPUx86.Mov { DestinationReg = CPUx86.RegistersEnum.EAX, DestinationIsIndirect = true, DestinationDisplacement = (int)(((mObjSize / 4) * 4) + 1), SourceValue = 0, Size = 16 };
            break;
          }
        case 0:
          break;
        default:
          {
            throw new NotImplementedException("Remainder size " + mObjSize % 4 + " not supported yet! (Type = '" + mType.FullName + "')");
          }
      }
    }
  }
}
