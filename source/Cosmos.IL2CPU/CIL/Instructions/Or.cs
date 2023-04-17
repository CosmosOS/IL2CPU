using System;
using Cosmos.IL2CPU.CIL.Utils;
using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.CIL.Instructions
{
    [OpCode(ILOpCode.Code.Or)]
    public class Or : ILOp
    {
        public Or(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xStackContent = aOpCode.StackPopTypes[0];
            var xStackContentSecond = aOpCode.StackPopTypes[1];
            var xStackContentSize = SizeOfType(xStackContent);
            var xStackContentSecondSize = SizeOfType(xStackContentSecond);
            var xSize = Math.Max(xStackContentSize, xStackContentSecondSize);

            if (Align(xStackContentSize, 4) != Align(xStackContentSecondSize, 4))
            {
                throw new NotSupportedException("Operands have different size!");
            }
            if (xSize > 8)
            {
                throw new NotImplementedException("StackSize>8 not supported");
            }

            if (xSize > 4)
            {
                // [ESP] is low part
                // [ESP + 4] is high part
                // [ESP + 8] is low part
                // [ESP + 12] is high part
                XS.Pop(EAX);
                XS.Pop(EDX);
                // [ESP] is low part
                // [ESP + 4] is high part
                XS.Or(ESP, EAX, destinationIsIndirect: true);
                XS.Or(ESP, EDX, destinationDisplacement: 4);
            }
            else
            {
                XS.Pop(EAX);
                XS.Or(ESP, EAX, destinationIsIndirect: true);
            }
        }
    }
}
