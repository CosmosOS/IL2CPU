using XSharp.Assembler;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Ret)]
    public class Ret : ILOp
    {
        public Ret(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            //TODO: Return
            Jump_End(aMethod);
            // Need to jump to end of method. Assembler can emit this label for now
            //XS.Jump(MethodFooterOp.EndOfMethodLabelNameNormal);
        }
    }
}
