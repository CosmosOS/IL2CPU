using System;
using XSharp;
using XSharp.Assembler;

using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Refanytype)]
    public class Refanytype : ILOp
    {
        public Refanytype(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            // we have object on stack, so type, address and want only the type to remain
            DoNullReferenceCheck(Assembler, true, 0);
            XS.Pop(EAX);
            XS.Push(EAX, isIndirect: true, displacement: 0);
            XS.Push(EAX, isIndirect: true, displacement: 4);
        }
    }
}
