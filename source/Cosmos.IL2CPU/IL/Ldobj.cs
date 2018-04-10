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

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            DoNullReferenceCheck(Assembler, DebugEnabled, 0);
            OpType xType = (OpType)aOpCode;
            DoAssemble(xType.Value);
        }

        public static void DoAssemble(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            XS.Pop(EAX);
            var xObjSize = SizeOfType(type);

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
                case 0:
                    {
                        break;
                    }
                default:
                    throw new Exception("Remainder not supported!");
            }

            xObjSize -= (xObjSize % 4);

            for (int i = 1; i <= (xObjSize / 4); i++)
            {
                XS.Push(EAX, displacement: (int)(xObjSize - (i * 4)));
            }
        }
    }
}
