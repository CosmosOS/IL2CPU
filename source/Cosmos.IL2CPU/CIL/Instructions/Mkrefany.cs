using System;
using Cosmos.IL2CPU.CIL.Utils;
using XSharp.Assembler;

namespace Cosmos.IL2CPU.CIL.Instructions
{
    public class Mkrefany : ILOp
    {
        public Mkrefany(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            throw new NotImplementedException();
        }
    }
}
