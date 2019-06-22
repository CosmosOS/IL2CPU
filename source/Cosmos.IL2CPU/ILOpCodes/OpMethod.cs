using System;

using IL2CPU.Reflection;
using static Cosmos.IL2CPU.TypeRefHelper;

namespace Cosmos.IL2CPU.ILOpCodes
{
  public class OpMethod : ILOpCode
  {
    public MethodInfo Value { get; set; }
    public uint ValueUID { get; set; }

    public OpMethod(Code aOpCode, int aPos, int aNextPos, MethodInfo aValue, ExceptionBlock aCurrentExceptionRegion)
      : base(aOpCode, aPos, aNextPos, aCurrentExceptionRegion)
    {
      Value = aValue;
    }

    public override int GetNumberOfStackPops(MethodInfo aMethod)
    {
      switch (OpCode)
      {
        case Code.Call:
        case Code.Callvirt:
          if (Value.IsStatic)
          {
            return Value.ParameterTypes.Count;
          }
          else
          {
            return Value.ParameterTypes.Count + 1;
          }
        case Code.Newobj:
          return Value.ParameterTypes.Count;
        case Code.Ldftn:
          return 0;
        case Code.Ldvirtftn:
          return 1;
        default:
          throw new NotImplementedException("OpCode '" + OpCode + "' not implemented!");
      }
    }

    public override int GetNumberOfStackPushes(MethodInfo aMethod)
    {
      switch (OpCode)
      {
        case Code.Call:
        case Code.Callvirt:
          var methodInfo = Value as MethodInfo;
          if (methodInfo != null && methodInfo.ReturnType != typeof(void))
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


    protected override void DoInitStackAnalysis(MethodInfo aMethod)
    {
      base.DoInitStackAnalysis(aMethod);

      switch (OpCode)
      {
        case Code.Call:
        case Code.Callvirt:
          var xMethodInfo = Value as MethodInfo;
          if (xMethodInfo != null && xMethodInfo.ReturnType != typeof(void))
          {
            StackPushTypes[0] = xMethodInfo.ReturnType;
            if (StackPushTypes[0].IsEnum)
            {
              StackPushTypes[0] = StackPushTypes[0].GetEnumUnderlyingType();
            }
          }
          break;
        //  var xExtraOffset = 0;
        //  if (!Value.IsStatic)
        //  {
        //    StackPopTypes[0] = Value.DeclaringType;
        //    xExtraOffset++;
        //  }
        //  var xParams = Value.GetParameters();
        //  for (int i = 0; i < xParams.Length; i++)
        //  {
        //    StackPopTypes[i + xExtraOffset] = xParams[i].ParameterType;
        //  }
        //  break;
        case Code.Newobj:
          StackPushTypes[0] = Value.DeclaringType;
          //  xParams = Value.GetParameters();
          //  for (int i = 0; i < xParams.Length; i++)
          //  {
          //    StackPopTypes[i] = xParams[i].ParameterType;
          //  }
          break;
        case Code.Ldftn:
          StackPushTypes[0] = TypeOf(BclType.IntPtr);
          return;

        default:
          break;
      }
    }
  }
}
