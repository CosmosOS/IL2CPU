using System;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Unaligned)]
    public class Unaligned : ILOp
    {
        public Unaligned(XSharp.Assembler.Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            //throw new NotImplementedException("TODO: Unaligned");
        }
    }
}
