using Cosmos.IL2CPU.ILOpCodes;
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

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            Assemble(Assembler, DebugEnabled);
        }

        public static void Assemble(Assembler aAssembler, bool debugEnabled, bool doNullReferenceCheck = true)
        {
            if (doNullReferenceCheck)
            {
                DoNullReferenceCheck(aAssembler, debugEnabled, 4);
            }

            XS.Add(RSP, 4);
            XS.Pop(RAX);

            XS.Push(RAX, displacement: 8);
        }
    }
}
