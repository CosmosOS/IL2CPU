using System;
using XSharp.Assembler;

namespace Cosmos.IL2CPU.IL
{
    [OpCode(ILOpCode.Code.Break)]
    public class Break : ILOp
    {
        public Break(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            throw new Exception("Cosmos.IL2CPU.x86->IL->Break.cs->Error: Break op-code has not been implemented yet!");
        }
    }
}
