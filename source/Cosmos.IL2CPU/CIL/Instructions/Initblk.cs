using Cosmos.IL2CPU.CIL.Utils;
using XSharp;
using XSharp.Assembler;
using XSharp.Assembler.x86;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.CIL.Instructions
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
            XS.Pop(ECX);
            // value
            XS.Pop(EBX);
            // address
            XS.Pop(EDI);

            XS.Compare(ECX, 0);
            XS.Jump(ConditionalTestEnum.Equal, xEndLabel);

            XS.Set(EDI, BL, destinationIsIndirect: true);

            XS.Compare(ECX, 1);
            XS.Jump(ConditionalTestEnum.Equal, xEndLabel);

            XS.Set(ESI, EDI);
            XS.Increment(EDI);
            XS.Decrement(ECX);

            new Movs { Prefixes = InstructionPrefixes.Repeat, Size = 8 };

            XS.Label(xEndLabel);
        }
    }
}
