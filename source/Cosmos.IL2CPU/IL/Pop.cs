using System;

using XSharp;
using CPUx86 = XSharp.Assembler.x86;

namespace Cosmos.IL2CPU.X86.IL
{
    [Cosmos.IL2CPU.OpCode(ILOpCode.Code.Pop)]
    public class Pop : ILOp
    {
        public Pop(XSharp.Assembler.Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            // todo: implement exception support.
            var xSize = SizeOfType(aOpCode.StackPopTypes[0]);
            XS.Add(XSRegisters.ESP, Align((uint)xSize, 4));
        }

    }
}
