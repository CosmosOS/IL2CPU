using XSharp;

namespace Cosmos.IL2CPU.IL
{
    [OpCode(ILOpCode.Code.Br)]
    public class Br : ILOp
    {
        public Br(XSharp.Assembler.Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            XS.Jump(AppAssembler.TmpBranchLabel(aMethod, aOpCode));
        }
    }
}
