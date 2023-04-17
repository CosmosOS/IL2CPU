using System;
using Cosmos.IL2CPU.CIL.Utils;

namespace Cosmos.IL2CPU.CIL.Instructions
{
    public class Cpobj : ILOp
    {
        public Cpobj(XSharp.Assembler.Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            throw new NotImplementedException("Cosmos.IL2CPU.x86->IL->Cpobj.cs->Error: The Cpobj op-code has not yet been implemented!");
        }
    }
}
