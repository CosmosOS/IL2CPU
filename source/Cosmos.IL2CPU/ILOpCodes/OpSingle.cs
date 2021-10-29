﻿using System;
using System.Reflection;

using static IL2CPU.Reflection.BaseTypeSystem;

namespace Cosmos.IL2CPU.ILOpCodes {
  public class OpSingle : ILOpCode {
    public float Value { get; }

    public OpSingle(Code aOpCode, int aPos, int aNextPos, float aValue, _ExceptionRegionInfo aCurrentExceptionRegion)
      : base(aOpCode, aPos, aNextPos, aCurrentExceptionRegion) {
      Value = aValue;
    }

    public override int GetNumberOfStackPops(MethodBase aMethod)
    {
      switch (OpCode)
      {
        case Code.Ldc_R4:
          return 0;
        default:
          throw new NotImplementedException("OpCode '" + OpCode + "' not implemented!");
      }
    }

    public override int GetNumberOfStackPushes(MethodBase aMethod)
    {
      switch (OpCode)
      {
        case Code.Ldc_R4:
          return 1;
        default:
          throw new NotImplementedException("OpCode '" + OpCode + "' not implemented!");
      }
    }

    protected override void DoInitStackAnalysis(MethodBase aMethod)
    {
      base.DoInitStackAnalysis(aMethod);

      switch (OpCode)
      {
        case Code.Ldc_R4:
          StackPushTypes[0] = BaseTypes.Single;
          return;
        default:
          break;
      }
    }

    public override void DoInterpretStackTypes() { }
  }
}
