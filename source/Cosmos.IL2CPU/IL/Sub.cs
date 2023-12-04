using System;
using CPUx86 = XSharp.Assembler.x86;
using XSharp.Assembler.x86;
using XSharp.Assembler.x86.x87;

using XSharp;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    [Cosmos.IL2CPU.OpCode(ILOpCode.Code.Sub)]
    public class Sub: ILOp
    {
        public Sub(XSharp.Assembler.Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xStackTop = aOpCode.StackPopTypes[0];
            var xStackTop2 = aOpCode.StackPopTypes[0];
            var xStackTopSize = SizeOfType(xStackTop);
            var xStackTop2Size = SizeOfType(xStackTop2);
            if (xStackTopSize != xStackTop2Size)
            {

                throw new Exception($"Different size for subtract: {aMethod.MethodBase}!, xStackTopSize={xStackTopSize} xStackTop2Size={xStackTop2Size}");
            }

            var xStackTopIsFloat = TypeIsFloat(xStackTop);

            switch (xStackTopSize)
            {
                case 1:
                case 2:
                case 4:
                    if (xStackTopIsFloat)
                    {
                        XS.SSE.MoveSS(XMM0, RSP, sourceIsIndirect: true);
                        XS.Add(RSP, 4);
                        XS.SSE.MoveSS(XMM1, RSP, sourceIsIndirect: true);
                        //XS.LiteralCode("movss XMM1, [ESP + 4]");
                        XS.SSE.SubSS(XMM1, XMM0);
                        XS.SSE.MoveSS(RSP, XMM1, destinationIsIndirect: true);
                    }
                    else
                    {
                        XS.Pop(XSRegisters.RCX);
                        XS.Pop(XSRegisters.RAX);
                        XS.Sub(XSRegisters.RAX, XSRegisters.RCX);
                        XS.Push(XSRegisters.RAX);
                    }
                    break;
                case 8:
                    if (xStackTopIsFloat)
                    {
                        XS.SSE2.MoveSD(XMM0, RSP, sourceIsIndirect: true);
                        XS.Add(RSP, 8);
                        XS.SSE2.MoveSD(XMM1, RSP, sourceIsIndirect: true);
                        XS.SSE2.SubSD(XMM1, XMM0);
                        XS.SSE2.MoveSD(RSP, XMM1, destinationIsIndirect: true);
                    }
                    else
                    {
                        XS.Pop(RAX);
                        XS.Pop(RDX);
                        XS.Sub(RSP, RAX, destinationIsIndirect: true);
                        XS.SubWithCarry(RSP, RDX, destinationDisplacement: 4);
                    }
                    break;
                default:
                    throw new NotImplementedException($"not implemented xStackTopSize={xStackTopSize}");
            }
        }
    }
}
