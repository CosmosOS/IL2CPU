using System;

using XSharp.Assembler;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Refanyval)]
    public class Refanyval : ILOp
    {
        public Refanyval(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            throw new NotImplementedException();
        }
    }
}
