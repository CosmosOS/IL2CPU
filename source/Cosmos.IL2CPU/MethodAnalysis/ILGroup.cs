using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IL2CPU.Debug.Symbols;

namespace Cosmos.IL2CPU.MethodAnalysis
{
    public class ILGroup
    {
        public static List<ILGroup> GenerateGroups(ILMethod aMethod, List<DebugInfo.SequencePoint> aSequences)
        {
            var analysed = 0;

            var groups = new List<ILGroup>();

            var toAnalyse = new Queue<ILOpCode>();
            toAnalyse.Enqueue(aMethod.First);

            while(toAnalyse.Count != 0)
            {
                var analysing = toAnalyse.Dequeue();
                analysed++;

                var group = groups.FirstOrDefault(g => g.OpCodes.Contains(analysing));
                if(group is null)
                {
                    group = new ILGroup(analysing);
                    groups.Add(group);
                }

                foreach (var future in analysing.GetNextOpCodePositions())
                {
                    var (newGroup, Position) = future;
                    if (!newGroup)
                    {
                        // we still have to check if we want this group to have a debug point at this position
                        newGroup = aSequences.Exists(q => q.Offset == analysing.Position && q.LineStart != 0xFEEFEE);
                    }

                    if (newGroup)
                    {
                        groups.Add(new ILGroup(aMethod.Code[Position]));
                    }
                }
            }

            if(analysed != aMethod.Length)
            {
                throw new Exception("GenerateGroups --- Did not reach all instructions in method");
            }

            return groups;
        }

        public List<ILOpCode> OpCodes;
        public List<int> PossibleContinuations;
        public int? StartPosition;
        public List<Type> StartStack = null;

        #region Constructors
        public ILGroup()
        {
            OpCodes = new List<ILOpCode>();
        }

        public ILGroup(ILOpCode aOpCode)
        {
            OpCodes = new List<ILOpCode> { aOpCode };
            StartPosition = aOpCode.Position;
        }

        public ILGroup(ILOpCode aOpCode, List<Type> aStack) : this(aOpCode)
        {
            StartStack = new List<Type>(aStack);
        }

        public ILGroup(List<Type> aStack)
        {
            StartStack = new List<Type>(aStack);
        }

        #endregion 

        public bool ReadyToAnalyse()
        {
            return StartPosition.HasValue && StartStack != null;
        }
    }
}
