using Cosmos.IL2CPU.CIL.Utils;
using XSharp;

namespace Cosmos.IL2CPU.CIL.Instructions
{
    public class Nop : ILOp
    {
        public Nop( XSharp.Assembler.Assembler aAsmblr )
            : base( aAsmblr )
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode )
        {
            XS.Noop();
        }

    }
}
