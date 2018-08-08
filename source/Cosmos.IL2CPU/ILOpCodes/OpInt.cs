using System;

using IL2CPU.Reflection;
using static Cosmos.IL2CPU.TypeRefHelper;

namespace Cosmos.IL2CPU.ILOpCodes {
  public class OpInt : ILOpCode {
    public int Value { get; }

    public OpInt(Code aOpCode, int aPos, int aNextPos, int aValue, ExceptionBlock aCurrentExceptionRegion)
      : base(aOpCode, aPos, aNextPos, aCurrentExceptionRegion) {
      Value = aValue;
    }

    public override int GetNumberOfStackPops(MethodInfo aMethod)
    {
      switch (OpCode)
      {
        case Code.Ldc_I4:
          return 0;
        case Code.Unaligned:
          return 0;
        default:
          throw new NotImplementedException("OpCode '" + OpCode + "' not implemented!");
      }
    }

    public override int GetNumberOfStackPushes(MethodInfo aMethod)
    {
      switch (OpCode)
      {
        case Code.Ldc_I4:
          return 1;
        case Code.Unaligned:
          return 0;
        default:
          throw new NotImplementedException("OpCode '" + OpCode + "' not implemented!");
      }
    }

    protected override void DoInitStackAnalysis(MethodInfo aMethod)
    {
      base.DoInitStackAnalysis(aMethod);

      switch (OpCode)
      {
        case Code.Ldc_I4:
          StackPushTypes[0] = TypeOf(BclType.Int32);
          return;
        default:
          break;
      }
    }
  }
}
