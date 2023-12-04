using XSharp;
using XSharp.Assembler;
using XSharp.Assembler.x86;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Initblk)]
    public class Initblk : ILOp
    {
        public Initblk(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xEndLabel = GetLabel(aMethod, aOpCode) + ".End";

            // size
            XS.Pop(RCX);
            // value
            XS.Pop(RBX);
            // address
            XS.Pop(RDI);

            XS.Compare(RCX, 0);
            XS.Jump(ConditionalTestEnum.Equal, xEndLabel);

            XS.Set(RDI, BL, destinationIsIndirect: true);

            XS.Compare(RCX, 1);
            XS.Jump(ConditionalTestEnum.Equal, xEndLabel);

            XS.Set(RSI, RDI);
            XS.Increment(RDI);
            XS.Decrement(RCX);

            new Movs { Prefixes = InstructionPrefixes.Repeat, Size = 8 };

            XS.Label(xEndLabel);
        }
    }
}
