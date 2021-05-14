using System;

using XSharp.Assembler;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Readonly)]
    public class Readonly : ILOp
    {
        public Readonly(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            XS.Comment("Readonly - for now do nothing");
            XS.Noop();
        }
    }
}
