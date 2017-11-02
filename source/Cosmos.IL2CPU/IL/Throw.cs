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

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            // TODO: Implement exception
            DoNullReferenceCheck(Assembler, DebugEnabled, 4);
            XS.Add(ESP, 4);
            XS.Pop(EAX);
            XS.Set(DataMember.GetStaticFieldName(ExceptionHelperRefs.CurrentExceptionRef), EAX, destinationIsIndirect: true);
            XS.Call("SystemExceptionOccurred");
            XS.Set(ECX, 3);
            EmitExceptionLogic(Assembler, aMethod, aOpCode, false, null);
        }
    }
}
