using Cosmos.IL2CPU.CIL.Utils;
using XSharp;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.CIL.Instructions
{
    [OpCode(ILOpCode.Code.Rethrow)]
    public class Rethrow : ILOp
    {
        public Rethrow(XSharp.Assembler.Assembler aAsmblr) : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            XS.Set(ECX, 3);
            EmitExceptionLogic(Assembler, aMethod, aOpCode, false, null);
        }
    }
}
