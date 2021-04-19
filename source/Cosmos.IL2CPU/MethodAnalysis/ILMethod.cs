using System.Collections.Generic;
using System.Linq;
using IL2CPU.Debug.Symbols;

namespace Cosmos.IL2CPU.MethodAnalysis
{
    public class ILMethod
    {
        public ILOpCode First;
        public Dictionary<int, ILOpCode> Code;
        public int Length;
        public List<ILGroup> StructuredCode;
        public ILMethod(List<ILOpCode> aOpCodes, DebugInfo.SequencePoint[] aSequences)
        {
            First = aOpCodes[0];
            Code = new Dictionary<int, ILOpCode>();
            foreach (var opCode in aOpCodes)
            {
                Code[opCode.Position] = opCode;
            }
            Length = aOpCodes.Count;
            StructuredCode = ILGroup.GenerateGroups(this, aSequences.ToList());
        }
    }
}
