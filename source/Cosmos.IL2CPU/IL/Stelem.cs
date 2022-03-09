using Cosmos.IL2CPU.ILOpCodes;

using XSharp.Assembler;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Stelem)]
    public class Stelem : ILOp
    {
        public Stelem(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xOpType = (OpType)aOpCode;
            var xSize = SizeOfType(xOpType.Value);

            Stelem_Ref.Assemble(Assembler, xSize, aMethod, aOpCode, DebugEnabled);
        }
    }
}
