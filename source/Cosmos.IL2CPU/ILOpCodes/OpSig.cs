using System;
using System.Reflection;

namespace Cosmos.IL2CPU.ILOpCodes
{
    public class OpSig : ILOpCode
    {
        public int Value { get; }

        public OpSig(Code aOpCode, int aPos, int aNextPos, int aValue, _ExceptionRegionInfo aCurrentExceptionRegion)
          : base(aOpCode, aPos, aNextPos, aCurrentExceptionRegion)
        {
            Value = aValue;
        }

        public override int GetNumberOfStackPops(MethodBase aMethod)
        {
            switch (OpCode)
            {
                case Code.Calli:
                    return 1;
                default:
                    throw new NotImplementedException("OpCode '" + OpCode + "' not implemented!");
            }
        }

        public override int GetNumberOfStackPushes(MethodBase aMethod)
        {
            switch (OpCode)
            {
                case Code.Calli:
                    return 0;
                default:
                    throw new NotImplementedException("OpCode '" + OpCode + "' not implemented!");
            }
        }

        public override void DoInterpretStackTypes() { }
    }
}
