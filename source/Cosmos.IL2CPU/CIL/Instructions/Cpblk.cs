using Cosmos.IL2CPU.CIL.Utils;
using XSharp;
using XSharp.Assembler.x86;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.CIL.Instructions
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
            XS.Pop(EDI);
            // source address
            XS.Pop(ESI);
            // byte count
            XS.Pop(ECX);

            new Movs { Prefixes = InstructionPrefixes.Repeat, Size = 8 };
        }
    }
}
