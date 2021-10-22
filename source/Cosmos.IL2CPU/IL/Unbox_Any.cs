namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Unbox_Any)]
    public class Unbox_Any : ILOp
    {
        public Unbox_Any(XSharp.Assembler.Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
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
