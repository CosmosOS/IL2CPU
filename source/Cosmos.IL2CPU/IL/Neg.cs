using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Neg)]
    public class Neg : ILOp
    {
        public Neg(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xStackContent = aOpCode.StackPopTypes[0];
            var xStackContentSize = SizeOfType(xStackContent);
            var xStackContentIsFloat = TypeIsFloat(xStackContent);
            if (xStackContentSize > 4)
            {
                if (xStackContentIsFloat)
                {
                    // There is no direct double negate instruction in SSE simply we do a XOR with 0x8000000000 to flip the sign bit
                    XS.SSE2.MoveSD(XMM0, RSP, sourceIsIndirect: true);
                    XS.SSE2.XorPD(XMM0, "__doublesignbit", sourceIsIndirect: true);
                    XS.SSE2.MoveSD(RSP, XMM0, destinationIsIndirect: true);
                }
                else
                {
                    XS.Pop(RBX); // low
                    XS.Pop(RAX); // high
                    XS.Negate(RBX); // set carry if EBX != 0
                    XS.AddWithCarry(RAX, 0);
                    XS.Negate(RAX);
                    XS.Push(RAX);
                    XS.Push(RBX);
                }
            }
            else
            {
                if (xStackContentIsFloat)
                {
                    // There is no direct float negate instruction in SSE simply we do a XOR with 0x80000000 to flip the sign bit
                    XS.SSE.MoveSS(XMM0, RSP, sourceIsIndirect: true);
                    XS.SSE.MoveSS(XMM1, "__floatsignbit", sourceIsIndirect: true);
                    XS.SSE.XorPS(XMM0, XMM1);
                    XS.SSE.MoveSS(RSP, XMM0, destinationIsIndirect: true);
                }
                else
                {
                    XS.Pop(RAX);
                    XS.Negate(RAX);
                    XS.Push(RAX);
                }
            }
        }
    }
}
