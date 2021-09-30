using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IL2CPU.Debug.Symbols;

namespace Cosmos.IL2CPU.MethodAnalysis
{
    public class ILGroup
    {
        public static List<ILGroup> GenerateGroups(ILMethod aMethod, DebugInfo.SequencePoint[] aSequences)
        {
            var analysed = 0;

            var groups = new Dictionary<int, ILGroup>();

            // Make lookup table from aSequences
            HashSet<int> sequenceLookup = null;
            if(aSequences.Length != 0)
            {
                sequenceLookup = new HashSet<int>();
                foreach (var seq in aSequences)
                {
                    if(seq.LineStart != 0xFEEFEE)
                    {
                        sequenceLookup.Add(seq.Offset);
                    }
                }
            }

            var first = new ILGroup(aMethod.First, new Stack<Type> { });
            groups.Add(aMethod.First.Position, first);

            _ExceptionRegionInfo exceptionRegion;

            // find all groups by finding all branches etc
            foreach (var position in aMethod.Code.Keys)
            {
                var op = aMethod.Code[position];
                exceptionRegion = op.CurrentExceptionRegion;

                if (exceptionRegion != null && exceptionRegion.HandlerOffset == position)
                {
                    if (!groups.ContainsKey(position))
                    {
                        var item = new ILGroup(op);
                        groups.Add(position, item);
                    }
                }

                foreach (var future in op.GetNextOpCodePositions())
                {
                    var (newGroup, Position) = future;

                    if (groups.ContainsKey(Position)) // branches sometimes force us to have more groups then expected
                    {
                        // this opcode should already be in the process of being analysed
                        continue;
                    }

                    if (!newGroup)
                    {
                        // if this is the first operation in a try block we also want a new group
                        newGroup = ((exceptionRegion?.TryOffset ?? -1) != (aMethod.Code[Position].CurrentExceptionRegion?.TryOffset ?? -1));
                        // we still have to check if we want this group to have a debug point at this position
                        newGroup |= (sequenceLookup != null && sequenceLookup.Contains(Position));
                        // also a new group if we reach the catch block
                        newGroup |= exceptionRegion != null && exceptionRegion.HandlerOffset == Position;
                    }


                    var aILOpCode = aMethod.Code[Position];
                    if (newGroup)
                    {
                        var item = new ILGroup(aILOpCode);
                        groups.Add(Position, item);
                    }
                }
            }

            // Initialse the datastructure with the first opcode

            // Analyse op codes
            foreach (var pair in groups)
            {
                var opGroup = pair.Value;
                while (true)
                {
                    var analysing = opGroup.OpCodes.Last();

                    analysed++;

                    var done = true;

                    if (analysing.CurrentExceptionRegion != null && analysing.CurrentExceptionRegion.TryOffset == analysing.Position)
                    {
                        opGroup.PossibleContinuations.Add(groups[analysing.CurrentExceptionRegion.HandlerOffset]);
                    }

                    foreach (var future in analysing.GetNextOpCodePositions())
                    {
                        var (newGroup, Position) = future;
                        if (!newGroup && !groups.ContainsKey(Position))
                        {
                            opGroup.OpCodes.Add(aMethod.Code[Position]);
                            done = false;
                        }
                        else
                        {
                            opGroup.PossibleContinuations.Add(groups[Position]);
                        }

                    }

                    if (done)
                    {
                        break;
                    }
                }
            }

            if(analysed != aMethod.Length)
            {
                throw new Exception("GenerateGroups --- Did not reach all instructions in method");
            }

            return groups.Values.ToList();
        }

        public List<ILOpCode> OpCodes;
        public List<ILGroup> PossibleContinuations;
        public int? StartPosition;
        public Stack<Type> StartStack = null;
        
        #region Constructors
        public ILGroup()
        {
            OpCodes = new List<ILOpCode>();
            PossibleContinuations = new List<ILGroup>();
        }

        public ILGroup(ILOpCode aOpCode)
        {
            OpCodes = new List<ILOpCode> { aOpCode };
            StartPosition = aOpCode.Position;
            PossibleContinuations = new List<ILGroup>();
        }

        public ILGroup(ILOpCode aOpCode, Stack<Type> aStack) : this(aOpCode)
        {
            StartStack = new Stack<Type>(aStack.Reverse());
        }

        public ILGroup(Stack<Type> aStack)
        {
            StartStack = new Stack<Type>(aStack.Reverse());
            PossibleContinuations = new List<ILGroup>();
        }

        #endregion 

        public bool ReadyToAnalyse()
        {
            return StartPosition.HasValue && StartStack != null;
        }

        public void Add(ILOpCode aILOpCode)
        {
            if(OpCodes.Count == 0)
            {
                StartPosition = aILOpCode.Position;
            }
            OpCodes.Add(aILOpCode);
        }
    }
}
