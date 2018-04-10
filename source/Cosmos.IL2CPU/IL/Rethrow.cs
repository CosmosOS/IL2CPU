using XSharp;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Rethrow)]
    public class Rethrow : ILOp
    {
        public Rethrow(XSharp.Assembler.Assembler aAsmblr) : base(aAsmblr)
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            XS.Set(ECX, 3);
            EmitExceptionLogic(Assembler, aMethod, aOpCode, false, null);
        }
    }
}
