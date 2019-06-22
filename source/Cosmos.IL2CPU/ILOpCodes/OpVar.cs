using System;

using IL2CPU.Reflection;

namespace Cosmos.IL2CPU.ILOpCodes
{
  public class OpVar : ILOpCode
  {
    public ushort Value { get; }

    public OpVar(Code aOpCode, int aPos, int aNextPos, ushort aValue, ExceptionBlock aCurrentExceptionRegion)
        : base(aOpCode, aPos, aNextPos, aCurrentExceptionRegion)
    {
      Value = aValue;
    }

    public override int GetNumberOfStackPops(MethodInfo aMethod)
    {
      switch (OpCode)
      {
        case Code.Ldloc:
        case Code.Ldloca:
        case Code.Ldarg:
        case Code.Ldarga:
          return 0;
        case Code.Stloc:
        case Code.Starg:
          return 1;
        default:
          throw new NotImplementedException("OpCode '" + OpCode + "' not implemented!");
      }
    }

    public override int GetNumberOfStackPushes(MethodInfo aMethod)
    {
      switch (OpCode)
      {
        case Code.Stloc:
        case Code.Starg:
          return 0;
        case Code.Ldloc:
        case Code.Ldloca:
        case Code.Ldarg:
        case Code.Ldarga:
          return 1;
        default:
          throw new NotImplementedException("OpCode '" + OpCode + "' not implemented!");
      }
    }

    protected override void DoInitStackAnalysis(MethodInfo aMethod)
    {
      base.DoInitStackAnalysis(aMethod);

      var xArgIndexCorrection = 0;
      var xParamTypes = aMethod.ParameterTypes;
      var xLocals = aMethod.MethodBody.LocalTypes;
      switch (OpCode)
      {
        case Code.Ldloc:
          StackPushTypes[0] = xLocals[Value];
          if (StackPushTypes[0].IsPinned)
          {
            StackPushTypes[0] = StackPushTypes[0].GetElementType();
          }
          if (StackPushTypes[0].IsEnum)
          {
            StackPushTypes[0] = StackPushTypes[0].GetEnumUnderlyingType();
          }
          return;
        case Code.Ldloca:
          StackPushTypes[0] = xLocals[Value];
          if (StackPushTypes[0].IsPinned)
          {
            StackPushTypes[0] = StackPushTypes[0].GetElementType();
          }
          StackPushTypes[0] = StackPushTypes[0].MakeByReferenceType();
          return;
        case Code.Ldarg:
          if (!aMethod.IsStatic)
          {
            if (Value == 0)
            {
              StackPushTypes[0] = aMethod.DeclaringType;
              if (StackPushTypes[0].IsEnum)
              {
                StackPushTypes[0] = StackPushTypes[0].GetEnumUnderlyingType();
              }
              else if (StackPushTypes[0].IsValueType)
              {
                StackPushTypes[0] = StackPushTypes[0].MakeByReferenceType();
              }
              return;
            }
            xArgIndexCorrection = -1;
          }
          StackPushTypes[0] = xParamTypes[Value + xArgIndexCorrection];
          if (StackPushTypes[0].IsEnum)
          {
            StackPushTypes[0] = StackPushTypes[0].GetEnumUnderlyingType();
          }
          return;
        case Code.Ldarga:
          if (!aMethod.IsStatic)
          {
            if (Value == 0)
            {
              if (StackPushTypes[0].IsValueType)
              {
                StackPushTypes[0] = StackPushTypes[0].MakeByReferenceType();
              }
              return;
            }
            xArgIndexCorrection = -1;
          }
          StackPushTypes[0] = xParamTypes[Value + xArgIndexCorrection];
          StackPushTypes[0] = StackPushTypes[0].MakeByReferenceType();
          return;
      }
    }
  }
}
