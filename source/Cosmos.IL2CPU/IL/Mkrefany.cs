using System;

using XSharp.Assembler;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Mkrefany)]
    public class Mkrefany : ILOp
    {
        public Mkrefany(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            throw new NotImplementedException();
        }
    }
}
