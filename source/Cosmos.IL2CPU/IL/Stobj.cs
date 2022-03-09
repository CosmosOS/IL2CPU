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

            XS.Set(ECX, ESP, sourceDisplacement: checked((int)xRoundedSize));

            for (int i = 0; i < (xFieldSize / 4); i++)
            {
                XS.Pop(EAX);
                XS.Set(ECX, EAX, destinationDisplacement: i * 4);
            }

            switch (xFieldSize % 4)
            {
                case 1:
                    {
                        XS.Pop(EAX);
                        XS.Set(ECX, AL, destinationDisplacement: checked((int)(xFieldSize / 4) * 4));
                        break;
                    }
                case 2:
                    {
                        XS.Pop(EAX);
                        XS.Set(ECX, AX, destinationDisplacement: checked((int)(xFieldSize / 4) * 4));
                        break;
                    }
                case 0:
                    {
                        break;
                    }
                default:
                    throw new Exception("Remainder size " + (xFieldSize % 4) + " not supported!");
            }

            XS.Add(ESP, 4);
        }
    }
}
