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

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            var xType = aOpCode.StackPopTypes[0];
            var xSize = SizeOfType(xType);

            if (xSize <= 4)
            {
                XS.Not(ESP, isIndirect: true);
            }
            else if (xSize <= 8)
            {
                XS.Not(ESP, isIndirect: true);
                XS.Not(ESP, displacement: 4);
            }
            else
            {
                throw new NotImplementedException("Cosmos.IL2CPU.x86->IL->Not.cs->Error: StackSize > 8 not supported");
            }
        }
    }
}
