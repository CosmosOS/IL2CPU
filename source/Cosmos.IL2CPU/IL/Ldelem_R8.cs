namespace Cosmos.IL2CPU.IL
{
    [global::Cosmos.IL2CPU.OpCode( ILOpCode.Code.Ldelem_R8 )]
    public class Ldelem_R8 : ILOp
    {
        public Ldelem_R8( XSharp.Assembler.Assembler aAsmblr )
            : base( aAsmblr )
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode )
        {
            Ldelem_Ref.Assemble(Assembler, 8, false, aMethod, aOpCode, DebugEnabled);
        }
    }
}
