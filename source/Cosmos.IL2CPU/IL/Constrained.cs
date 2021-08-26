using Cosmos.IL2CPU.ILOpCodes;
using XSharp;
using XSharp.Assembler;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Constrained)]
    public class Constrained : ILOp
    {
        public Constrained(Assembler aAsmblr) : base(aAsmblr)
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            var xOpType = aOpCode as OpType;
            DoExecute(Assembler, aMethod, aOpCode, xOpType, DebugEnabled);
        }

        private void DoExecute(Assembler assembler, _MethodInfo aMethod, ILOpCode aOpCode, OpType aTargetType, bool debugEnabled)
        {
            XS.Comment($"Type = {aTargetType.Value}");
        }
    }
}
