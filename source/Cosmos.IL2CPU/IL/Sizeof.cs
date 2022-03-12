using Cosmos.IL2CPU.ILOpCodes;

using XSharp.Assembler;
using XSharp;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Sizeof)]
    public class Sizeof : ILOp
    {
        public Sizeof(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xType = (OpType)aOpCode;
            var xSize = SizeOfType(xType.Value);

            XS.Push(xSize);
        }
    }
}
