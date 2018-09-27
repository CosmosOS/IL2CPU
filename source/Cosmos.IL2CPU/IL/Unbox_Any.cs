using System;

using IL2CPU.API;
using Cosmos.IL2CPU.ILOpCodes;

using XSharp;
using XSharp.Assembler.x86;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Unbox_Any)]
    public class Unbox_Any : ILOp
    {
        public Unbox_Any(XSharp.Assembler.Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            new Unbox(Assembler).Execute(aMethod, aOpCode);
            new Ldobj(Assembler).Execute(aMethod, aOpCode);
        }
    }
}
