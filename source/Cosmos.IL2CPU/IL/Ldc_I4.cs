using Cosmos.IL2CPU.ILOpCodes;

using XSharp;
using XSharp.Assembler;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Ldc_I4)]
    public class Ldc_I4 : ILOp
    {
        public Ldc_I4(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            XS.Push((uint)((OpInt)aOpCode).Value);
        }
    }
}

