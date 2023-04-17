namespace Cosmos.IL2CPU.IL
{
    [OpCode(ILOpCode.Code.Unaligned)]
    public class Unaligned : ILOp
    {
        public Unaligned(XSharp.Assembler.Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            //throw new NotImplementedException("TODO: Unaligned");
        }
    }
}
