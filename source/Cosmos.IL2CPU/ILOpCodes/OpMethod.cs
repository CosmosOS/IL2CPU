using System;
using System.Reflection;
using static IL2CPU.Reflection.BaseTypeSystem;

namespace Cosmos.IL2CPU.ILOpCodes
{
  public class OpMethod : ILOpCode
  {
    public MethodBase Value { get; set; }
    public uint ValueUID { get; set; }

    public OpMethod(Code aOpCode, int aPos, int aNextPos, MethodBase aValue, _ExceptionRegionInfo aCurrentExceptionRegion)
      : base(aOpCode, aPos, aNextPos, aCurrentExceptionRegion)
    {
      Value = aValue;
    }

    public override int GetNumberOfStackPops(MethodBase aMethod)
    {
      switch (OpCode)
      {
        case Code.Call:
        case Code.Callvirt:
          if (Value.IsStatic)
          {
            return Value.GetParameters().Length;
          }
          else
          {
            return Value.GetParameters().Length + 1;
          }
        case Code.Newobj:
          return Value.GetParameters().Length;
        case Code.Ldftn:
          return 0;
        case Code.Ldvirtftn:
          return 1;
        default:
          throw new NotImplementedException("OpCode '" + OpCode + "' not implemented!");
      }
    }

    public override int GetNumberOfStackPushes(MethodBase aMethod)
    {
      switch (OpCode)
      {
        case Code.Call:
        case Code.Callvirt:
          var methodInfo = Value as MethodInfo;
          if (methodInfo != null && methodInfo.ReturnType != BaseTypes.Void)
          {
            return 1;
          }
          return 0;
        case Code.Newobj:
          return 1;
        case Code.Ldftn:
          return 1;
        case Code.Ldvirtftn:
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
        case Code.Call:
        case Code.Callvirt:
          var xMethodInfo = Value as MethodInfo;
          if (xMethodInfo != null && xMethodInfo.ReturnType != BaseTypes.Void)
          {
            StackPushTypes[0] = xMethodInfo.ReturnType;
            if (StackPushTypes[0].IsEnum)
            {
              StackPushTypes[0] = StackPushTypes[0].GetEnumUnderlyingType();
            }
          }
          break;
        case Code.Newobj:
          StackPushTypes[0] = Value.DeclaringType;
          break;
        case Code.Ldftn:
          StackPushTypes[0] = BaseTypes.IntPtr;
          return;

        default:
          break;
      }
    }

    public override void DoInterpretStackTypes() { }
  }
}
