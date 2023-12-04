using System;

using XSharp;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Not)]
    public class Not : ILOp
    {
        public Not(XSharp.Assembler.Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xType = aOpCode.StackPopTypes[0];
            var xSize = SizeOfType(xType);

            if (xSize <= 4)
            {
                XS.Not(RSP, isIndirect: true, size: RegisterSize.Long64);
            }
            else if (xSize <= 8)
            {
                XS.Not(RSP, isIndirect: true, size: RegisterSize.Long64);
                XS.Not(RSP, displacement: 4, size: RegisterSize.Long64);
            }
            else
            {
                throw new NotImplementedException("Cosmos.IL2CPU.x86->IL->Not.cs->Error: StackSize > 8 not supported");
            }
        }
    }
}
