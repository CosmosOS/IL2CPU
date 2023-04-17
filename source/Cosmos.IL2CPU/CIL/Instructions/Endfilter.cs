namespace Cosmos.IL2CPU.CIL.Instructions
{
    [OpCode(ILOpCode.Code.Endfilter)]
    public class Endfilter : ILOp
    {
        public Endfilter(XSharp.Assembler.Assembler aAsmblr) : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            //todo actually do this correctly
            //should pop one int and then either go to finally block or go to catch block
        }


    }
}
