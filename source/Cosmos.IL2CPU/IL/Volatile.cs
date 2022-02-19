namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Volatile)]
    public class Volatile : ILOp
    {
        public Volatile(XSharp.Assembler.Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            //throw new NotImplementedException("TODO: Volatile");
        }
    }
}
