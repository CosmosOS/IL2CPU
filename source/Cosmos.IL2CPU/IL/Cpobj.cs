using System;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Cpobj)]
    public class Cpobj : ILOp
    {
        public Cpobj(XSharp.Assembler.Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            throw new NotImplementedException("Cosmos.IL2CPU.x86->IL->Cpobj.cs->Error: The Cpobj op-code has not yet been implemented!");
        }
    }
}
