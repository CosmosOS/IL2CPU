using IL2CPU.API;

using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Throw)]
    public class Throw : ILOp
    {
        public Throw(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            // TODO: Implement exception
            DoNullReferenceCheck(Assembler, DebugEnabled, 4);
            XS.Add(RSP, 4);
            XS.Pop(RAX);
            XS.Set(LabelName.GetStaticFieldName(ExceptionHelperRefs.CurrentExceptionRef), RAX, destinationIsIndirect: true);
            XS.Call("SystemExceptionOccurred");
            XS.Set(RCX, 3);
            EmitExceptionLogic(Assembler, aMethod, aOpCode, false, null);

            // FIXME: This is only temporary, but this is better to avoid potential CPU faults if the code still gets executed (aka the exception was not handled before)
            var xBaseLabel = GetLabel(aMethod, aOpCode);
            var xLoop = xBaseLabel + "_Loop";

            XS.Label(xLoop);
            XS.Halt();
            XS.Jump(xLoop);
        }
    }
}
