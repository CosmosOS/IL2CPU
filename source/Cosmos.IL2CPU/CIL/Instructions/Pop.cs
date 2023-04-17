using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.CIL.Instructions
{
    [OpCode(ILOpCode.Code.Pop)]
    public class Pop : ILOp
    {
        public Pop(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            // todo: implement exception support.
            var xSize = SizeOfType(aOpCode.StackPopTypes[0]);
            XS.Add(ESP, Align(xSize, 4));
        }
    }
}
