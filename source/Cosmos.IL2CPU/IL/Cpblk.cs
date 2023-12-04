using XSharp;
using XSharp.Assembler.x86;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Cpblk)]
    public class Cpblk : ILOp
    {
        public Cpblk(XSharp.Assembler.Assembler aAsmblr) : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            // destination address
            XS.Pop(RDI);
            // source address
            XS.Pop(RSI);
            // byte count
            XS.Pop(RCX);

            new Movs { Prefixes = InstructionPrefixes.Repeat, Size = 8 };
        }
    }
}
