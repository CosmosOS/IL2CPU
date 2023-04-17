using Cosmos.IL2CPU.CIL.Utils;

namespace Cosmos.IL2CPU.CIL.Instructions
{
    public class Ldelem_I4 : ILOp
    {
        public Ldelem_I4( XSharp.Assembler.Assembler aAsmblr )
            : base( aAsmblr )
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode )
        {
            Ldelem_Ref.Assemble(Assembler, 4, true, aMethod, aOpCode, DebugEnabled);
        }
    }
}