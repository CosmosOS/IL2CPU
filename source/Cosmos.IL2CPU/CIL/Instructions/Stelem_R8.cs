namespace Cosmos.IL2CPU.CIL.Instructions
{
    [OpCode( ILOpCode.Code.Stelem_R8 )]
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
