using System;
using Cosmos.IL2CPU.CIL.Utils;
using XSharp.Assembler;

namespace Cosmos.IL2CPU.CIL.Instructions
{
    [OpCode(ILOpCode.Code.Arglist)]
    public class Arglist : ILOp
    {
        public Arglist(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            throw new NotImplementedException("Cosmos.IL2CPU.x86->IL->Arglist.cs->Error: Arglist not yet implemented!");
        }
    }
}
