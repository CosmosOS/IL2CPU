using System;
using Cosmos.IL2CPU.CIL.Utils;
using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.CIL.Instructions
{
    public class And : ILOp
    {
        public And(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xStackContent = aOpCode.StackPopTypes[0];
            var xStackContentSize = SizeOfType(xStackContent);
            var xStackContent2 = aOpCode.StackPopTypes[1];
            var xStackContent2Size = SizeOfType(xStackContent2);

            var xSize = Math.Max(xStackContentSize, xStackContent2Size);
            if (Align(xStackContentSize, 4) != Align(xStackContent2Size, 4))
            {
                throw new NotSupportedException("Cosmos.IL2CPU.x86->IL->And.cs->Error: Operands have different size!");
            }
            if (xSize > 8)
            {
                throw new NotImplementedException("Cosmos.IL2CPU.x86->IL->And.cs->Error: StackSize > 8 not supported");
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
                XS.And(ESP, EAX, destinationIsIndirect: true);
                XS.And(ESP, EDX, destinationDisplacement: 4);
            }
            else
            {
                XS.Pop(EAX);
                XS.And(ESP, EAX, destinationIsIndirect: true);
            }
        }
    }
}
