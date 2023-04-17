using XSharp;
using XSharp.Assembler;

namespace Cosmos.IL2CPU.IL
{
    [OpCode(ILOpCode.Code.Readonly)]
    public class Readonly : ILOp
    {
        public Readonly(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            XS.Comment("Readonly - for now do nothing");
            XS.Noop();
        }
    }
}
