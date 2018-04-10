using XSharp;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Ldnull)]
    public class Ldnull : ILOp
    {
        public Ldnull(XSharp.Assembler.Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            XS.Push(0);
            XS.Push(0);
        }
    }
}
