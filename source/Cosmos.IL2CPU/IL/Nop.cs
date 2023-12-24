using System;

using XSharp;
using CPU = XSharp.Assembler.x86;

namespace Cosmos.IL2CPU.X86.IL
{
    [Cosmos.IL2CPU.OpCode( ILOpCode.Code.Nop )]
    public class Nop : ILOp
    {
        public Nop( XSharp.Assembler.Assembler aAsmblr )
            : base( aAsmblr )
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode )
        {
            // we might have to emit a noop for the debugger to work
        }

    }
}
