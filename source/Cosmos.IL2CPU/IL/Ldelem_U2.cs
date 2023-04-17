namespace Cosmos.IL2CPU.IL
{
    [global::Cosmos.IL2CPU.OpCode( ILOpCode.Code.Ldelem_U2 )]
    public class Ldelem_U2 : ILOp
    {
        public Ldelem_U2( XSharp.Assembler.Assembler aAsmblr )
            : base( aAsmblr )
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode )
        {
            Ldelem_Ref.Assemble(Assembler, 2, false, aMethod, aOpCode, DebugEnabled);
        }
    }
}
