using System;
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
            StructuredCode = ILGroup.GenerateGroups(this, aSequences);
        }

        public void Analyse()
        {
            var toAnalyse = new List<ILGroup>(StructuredCode);
            while (toAnalyse.Count != 0)
            {
                var analysing = toAnalyse.First(g => g.StartStack != null);
                toAnalyse.Remove(analysing);
                var stack = new Stack<Type>(analysing.StartStack.Reverse());

                uint stackOffset = 0;
                foreach (var item in stack)
                {
                    stackOffset += ILOp.Align(ILOp.SizeOfType(item), 4);
                }

                foreach (var opcode in analysing.OpCodes)
                {
                    if(opcode.CurrentExceptionRegion != null && opcode.CurrentExceptionRegion.TryOffset == opcode.Position)
                    {
                        analysing.PossibleContinuations.First(c => c.StartPosition == opcode.CurrentExceptionRegion.HandlerOffset).StartStack = new Stack<Type>(analysing.StartStack.Reverse());
                    }

                    opcode.DoStackAnalysis(stack, ref stackOffset);
                }
                foreach (var c in analysing.PossibleContinuations )
                {
                    c.StartStack = new Stack<Type>(stack.Reverse());
                }
            }
        }
    }
}
