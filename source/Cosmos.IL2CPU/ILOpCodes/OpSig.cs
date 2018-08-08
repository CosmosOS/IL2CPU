using System;
using System.Reflection.Metadata;

using IL2CPU.Reflection;

namespace Cosmos.IL2CPU.ILOpCodes
{
    public class OpSig : ILOpCode
    {
        public MethodSignature<TypeInfo> Value { get; }

        public OpSig(Code aOpCode, int aPos, int aNextPos, MethodSignature<TypeInfo> aValue, ExceptionBlock aCurrentExceptionRegion)
            : base(aOpCode, aPos, aNextPos, aCurrentExceptionRegion)
        {
            Value = aValue;
        }

        public override int GetNumberOfStackPops(MethodInfo aMethod)
        {
            switch (OpCode)
            {
                default:
                    throw new NotImplementedException("OpCode '" + OpCode + "' not implemented!");
            }
        }

        public override int GetNumberOfStackPushes(MethodInfo aMethod)
        {
            switch (OpCode)
            {
                default:
                    throw new NotImplementedException("OpCode '" + OpCode + "' not implemented!");
            }
        }
    }
}
