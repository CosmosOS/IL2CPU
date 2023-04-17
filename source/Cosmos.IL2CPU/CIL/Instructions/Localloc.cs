using Cosmos.IL2CPU.CIL.Utils;
using Cosmos.IL2CPU.Cosmos;
using XSharp.Assembler;

namespace Cosmos.IL2CPU.CIL.Instructions
{
    public class Localloc : ILOp
    {
        public Localloc(Assembler aAsmblr)
          : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            string xCurrentMethodLabel = GetLabel(aMethod, aOpCode);
            Call.DoExecute(Assembler, aMethod, GCImplementationRefs.AllocNewObjectRef, aOpCode, xCurrentMethodLabel, DebugEnabled);
        }
    }
}
