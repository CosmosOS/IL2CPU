using Cosmos.IL2CPU.CIL.Utils;
using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.CIL.Instructions
{
    [OpCode(ILOpCode.Code.Refanytype)]
    public class Refanytype : ILOp
    {
        public Refanytype(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            // we have object on stack, so type, address and want only the type to remain
            DoNullReferenceCheck(Assembler, DebugEnabled, 0);
            XS.Pop(EAX);
            XS.Exchange(BX, BX); //TODO: Are we sure that we want to push a long? Isnt the type only an int?
            XS.Push(EAX, isIndirect: true, displacement: 0);
            XS.Push(EAX, isIndirect: true, displacement: 4);
        }
    }
}
