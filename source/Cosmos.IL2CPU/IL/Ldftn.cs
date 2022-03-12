using IL2CPU.API;
using Cosmos.IL2CPU.ILOpCodes;

using XSharp;
using XSharp.Assembler;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Ldftn)]
    public class Ldftn : ILOp
    {
        public Ldftn(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            XS.Push(LabelName.Get(((OpMethod)aOpCode).Value));
        }
    }
}
