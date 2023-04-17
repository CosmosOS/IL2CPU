using Cosmos.IL2CPU.CIL.ILOpCodes;
using Cosmos.IL2CPU.CIL.Utils;
using XSharp;
using XSharp.Assembler;

namespace Cosmos.IL2CPU.CIL.Instructions
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
