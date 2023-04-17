namespace Cosmos.IL2CPU.CIL.Instructions
{
    [OpCode( ILOpCode.Code.Ldelem_R4 )]
    public class Ldelem_R4 : ILOp
    {
        public Ldelem_R4( XSharp.Assembler.Assembler aAsmblr )
            : base( aAsmblr )
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode )
        {
            Ldelem_Ref.Assemble(Assembler, 4, false, aMethod, aOpCode, DebugEnabled);
        }
    }
}
