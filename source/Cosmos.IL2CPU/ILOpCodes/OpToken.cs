using System;
using System.Collections.Generic;

using IL2CPU.Reflection;
using static Cosmos.IL2CPU.TypeRefHelper;

namespace Cosmos.IL2CPU.ILOpCodes {
  public class OpToken : ILOpCode {
    public int Value { get; }
    public FieldInfo ValueField { get; }
    public TypeInfo ValueType { get; }

    public bool ValueIsType
    {
        get
        {
            if ((Value & 0x02000000) != 0)
            {
                return true;
            }
            if ((Value & 0x01000000) != 0)
            {
                return true;
            }
            if ((Value & 0x1B000000) != 0)
            {
                return true;
            }
            return false;
        }
    }
    public bool ValueIsField
    {
        get
        {
            if ((Value & 0x04000000) != 0)
            {
                return true;
            }
            return false;
        }
    }

    public OpToken(Code aOpCode, int aPos, int aNextPos, int aValue,ModuleInfo aModule,
      IReadOnlyList<TypeInfo> aTypeGenericArgs, IReadOnlyList<TypeInfo> aMethodGenericArgs, ExceptionBlock aCurrentExceptionRegion)
      : base(aOpCode, aPos, aNextPos, aCurrentExceptionRegion) {
      Value = aValue;
      if (ValueIsField)
      {
          ValueField = aModule.ResolveField(Value, aTypeGenericArgs, aMethodGenericArgs);
      }
      if (ValueIsType)
      {
          ValueType = aModule.ResolveType(Value, aTypeGenericArgs, aMethodGenericArgs);
      }

    }

    public override int GetNumberOfStackPops(MethodInfo aMethod)
    {
      switch (OpCode)
      {
        case Code.Ldtoken:
          return 0;
        default:
          throw new NotImplementedException("OpCode '" + OpCode + "' not implemented!");
      }
    }

    public override int GetNumberOfStackPushes(MethodInfo aMethod)
    {
      switch (OpCode)
      {
        case Code.Ldtoken:
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
        case Code.Ldtoken:
          if (ValueIsField)
          {
            StackPushTypes[0] = TypeOf(typeof(RuntimeFieldHandle));
          }
          else if (ValueIsType)
          {
            StackPushTypes[0] = TypeOf(typeof(RuntimeTypeHandle));
          }
          else
          {
            throw new NotImplementedException();
          }
          return;
        default:
          break;
      }
    }
  }
}
