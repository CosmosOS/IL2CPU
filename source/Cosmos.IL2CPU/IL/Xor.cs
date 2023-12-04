using System;

using XSharp;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Xor)]
    public class Xor : ILOp
    {
        public Xor(XSharp.Assembler.Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xType = aOpCode.StackPopTypes[0];
            var xSize = SizeOfType(xType);

            if (xSize <= 4)
            {
                XS.Pop(RAX);
                XS.Xor(RSP, RAX, destinationIsIndirect: true);
            }
            else if (xSize <= 8)
            {
                XS.Pop(RAX);
                XS.Pop(RDX);
                XS.Xor(RSP, RAX, destinationIsIndirect: true);
                XS.Xor(RSP, RDX, destinationDisplacement: 4);
            }
            else
            {
                throw new NotImplementedException("Cosmos.IL2CPU.x86->IL->Xor.cs->Error: StackSize > 8 not supported");
            }
        }
    }
}
