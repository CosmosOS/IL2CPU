namespace Cosmos.IL2CPU.CIL.Instructions
{
    [OpCode( ILOpCode.Code.Ldelem_I )]
    public class Ldelem_I : ILOp
    {
        public Ldelem_I( XSharp.Assembler.Assembler aAsmblr )
            : base( aAsmblr )
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode )
        {
            Ldelem_Ref.Assemble(Assembler, 4, true, aMethod, aOpCode, DebugEnabled);
        }
    }
}
