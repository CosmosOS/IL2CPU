using Cosmos.IL2CPU.CIL.Utils;
using XSharp;

namespace Cosmos.IL2CPU.CIL.Instructions
{
    public class Ldnull : ILOp
    {
        public Ldnull(XSharp.Assembler.Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            XS.Push(0);
            XS.Push(0);
        }
    }
}
