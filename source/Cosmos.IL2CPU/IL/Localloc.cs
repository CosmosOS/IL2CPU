using XSharp.Assembler;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Localloc)]
    public class Localloc : ILOp
    {
        public Localloc(Assembler aAsmblr)
          : base(aAsmblr)
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            //TODO: free heap in method footer.
            string xCurrentMethodLabel = GetLabel(aMethod, aOpCode);
            Call.DoExecute(Assembler, aMethod, GCImplementationRefs.AllocNewObjectRef, aOpCode, xCurrentMethodLabel, DebugEnabled);
        }
    }
}
