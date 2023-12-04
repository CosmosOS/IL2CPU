using System;

using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Stobj)]
    public class Stobj : ILOp
    {
        public Stobj(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xFieldSize = SizeOfType(aOpCode.StackPopTypes[0]);
            var xRoundedSize = Align(xFieldSize, 4);
            DoNullReferenceCheck(Assembler, DebugEnabled, (int)xRoundedSize);

            XS.Set(RCX, RSP, sourceDisplacement: checked((int)xRoundedSize));

            for (int i = 0; i < xFieldSize / 4; i++)
            {
                XS.Pop(RAX);
                XS.Set(RCX, RAX, destinationDisplacement: i * 4);
            }

            switch (xFieldSize % 4)
            {
                case 1:
                    {
                        XS.Pop(RAX);
                        XS.Set(RCX, AL, destinationDisplacement: checked((int)(xFieldSize / 4) * 4));
                        break;
                    }
                case 2:
                    {
                        XS.Pop(RAX);
                        XS.Set(RCX, AX, destinationDisplacement: checked((int)(xFieldSize / 4) * 4));
                        break;
                    }
                case 0:
                    {
                        break;
                    }
                default:
                    throw new Exception("Remainder size " + xFieldSize % 4 + " not supported!");
            }

            XS.Add(RSP, 4);
        }
    }
}
