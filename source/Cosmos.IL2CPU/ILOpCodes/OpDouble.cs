using System;

using IL2CPU.Reflection;
using static Cosmos.IL2CPU.TypeRefHelper;

namespace Cosmos.IL2CPU.ILOpCodes {
  public class OpDouble : ILOpCode {
    public double Value { get; }

    public OpDouble(Code aOpCode, int aPos, int aNextPos, double aValue, ExceptionBlock aCurrentExceptionRegion)
      : base(aOpCode, aPos, aNextPos, aCurrentExceptionRegion) {
      Value = aValue;
    }

    public override int GetNumberOfStackPops(MethodInfo aMethod)
    {
      switch (OpCode)
      {
        case Code.Ldc_R8:
          return 0;
        default:
          throw new NotImplementedException("OpCode '" + OpCode + "' not implemented!");
      }
    }

    public override int GetNumberOfStackPushes(MethodInfo aMethod)
    {
      switch (OpCode)
      {
        case Code.Ldc_R8:
          return 1;
        default:
          throw new NotImplementedException("OpCode '" + OpCode + "' not implemented!");
      }
    }

    protected override void DoInitStackAnalysis(MethodInfo aMethod)
    {
      base.DoInitStackAnalysis(aMethod);

      switch (OpCode)
      {
        case Code.Ldc_R8:
          StackPushTypes[0] = TypeOf(BclType.Double);
          break;
        default:
          break;
      }
    }
  }
}
