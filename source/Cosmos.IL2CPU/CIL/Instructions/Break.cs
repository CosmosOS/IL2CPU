using System;
using Cosmos.IL2CPU.CIL.Utils;
using XSharp.Assembler;

namespace Cosmos.IL2CPU.CIL.Instructions
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
