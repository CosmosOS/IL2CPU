using System;

using CPUx86 = XSharp.Assembler.x86;

namespace Cosmos.IL2CPU.X86.IL
{
    [Cosmos.IL2CPU.OpCode( ILOpCode.Code.Stelem_R8 )]
    public class Stelem_R8 : ILOp
    {
        public Stelem_R8( XSharp.Assembler.Assembler aAsmblr )
            : base( aAsmblr )
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode )
        {
            Stelem_Ref.Assemble(Assembler, 8, aMethod, aOpCode, DebugEnabled);
        }

    }
}
