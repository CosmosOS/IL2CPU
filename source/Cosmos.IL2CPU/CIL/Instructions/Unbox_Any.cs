using Cosmos.IL2CPU.CIL.Utils;

namespace Cosmos.IL2CPU.CIL.Instructions
{
    [OpCode(ILOpCode.Code.Unbox_Any)]
    public class Unbox_Any : ILOp
    {
        public Unbox_Any(XSharp.Assembler.Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            if (IsReferenceType(((ILOpCodes.OpType)aOpCode).Value))
            {
                return;
            }

            new Unbox(Assembler).Execute(aMethod, aOpCode);
            new Ldobj(Assembler).Execute(aMethod, aOpCode);
        }
    }
}
