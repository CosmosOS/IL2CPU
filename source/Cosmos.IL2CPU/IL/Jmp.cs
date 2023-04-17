using System;
using XSharp.Assembler;

namespace Cosmos.IL2CPU.IL
{
    [OpCode(ILOpCode.Code.Jmp)]
    public class Jmp : ILOp
    {
        public Jmp(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            throw new NotImplementedException();
        }
    }
}
