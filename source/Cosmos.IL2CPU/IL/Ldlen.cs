using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Ldlen)]
    public class Ldlen : ILOp
    {
        public Ldlen(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            DoNullReferenceCheck(Assembler, DebugEnabled, 4);

            XS.Add(ESP, 4);
            XS.Pop(EAX);

            XS.Push(EAX, displacement: 8);
        }
    }
}
