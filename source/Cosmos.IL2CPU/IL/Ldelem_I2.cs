using System;


namespace Cosmos.IL2CPU.X86.IL
{
    [Cosmos.IL2CPU.OpCode( ILOpCode.Code.Ldelem_I2 )]
    public class Ldelem_I2 : ILOp
    {
        public Ldelem_I2( XSharp.Assembler.Assembler aAsmblr )
            : base( aAsmblr )
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode )
        {
            Ldelem_Ref.Assemble(Assembler, 2, true, aMethod, aOpCode, DebugEnabled);
        }
    }
}
