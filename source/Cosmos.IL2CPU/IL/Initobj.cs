using System;
using IL2CPU.API;
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

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            Execute(aMethod, aOpCode);

        }

        public void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode, bool aFromNewObj = false)
        {
            Type mType = ((ILOpCodes.OpType)aOpCode).Value;
            uint mObjSize = SizeOfType(mType);

            XS.Pop(EAX);

            for (int i = 0; i < mObjSize / 4; i++)
            {
                XS.Set(EAX, 0, destinationDisplacement: i * 4, size: RegisterSize.Int32);
            }
            switch (mObjSize % 4)
            {
                case 1:
                    {
                        new CPUx86.Mov { DestinationReg = CPUx86.RegistersEnum.EAX, DestinationIsIndirect = true, DestinationDisplacement = (int)(mObjSize / 4 * 4), SourceValue = 0, Size = 8 };
                        break;
                    }
                case 2:
                    {
                        new CPUx86.Mov { DestinationReg = CPUx86.RegistersEnum.EAX, DestinationIsIndirect = true, DestinationDisplacement = (int)(mObjSize / 4 * 4), SourceValue = 0, Size = 16 };
                        break;
                    }
                case 3:
                    {
                        new CPUx86.Mov { DestinationReg = CPUx86.RegistersEnum.EAX, DestinationIsIndirect = true, DestinationDisplacement = (int)(mObjSize / 4 * 4), SourceValue = 0, Size = 8 };
                        new CPUx86.Mov { DestinationReg = CPUx86.RegistersEnum.EAX, DestinationIsIndirect = true, DestinationDisplacement = (int)(mObjSize / 4 * 4 + 1), SourceValue = 0, Size = 16 };
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
