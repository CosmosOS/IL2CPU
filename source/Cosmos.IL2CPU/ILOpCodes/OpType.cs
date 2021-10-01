using System;
using System.Reflection;
using Cosmos.IL2CPU.Extensions;
using static IL2CPU.Reflection.BaseTypeSystem;

namespace Cosmos.IL2CPU.ILOpCodes
{
  public class OpType : ILOpCode
  {
    public Type Value { get; }

    public OpType(Code aOpCode, int aPos, int aNextPos, Type aValue, _ExceptionRegionInfo aCurrentExceptionRegion)
      : base(aOpCode, aPos, aNextPos, aCurrentExceptionRegion)
    {
      Value = aValue;
    }

    public override int GetNumberOfStackPops(MethodBase aMethod)
    {
      switch (OpCode)
      {
        case Code.Initobj:
          return 1;
        case Code.Ldelema:
          return 2;
        case Code.Newarr:
          return 1;
        case Code.Box:
          return 1;
        case Code.Stelem:
          return 3;
        case Code.Ldelem:
          return 2;
        case Code.Isinst:
          return 1;
        case Code.Castclass:
          return 1;
        case Code.Constrained:
          return 0;
        case Code.Unbox_Any:
          return 1;
        case Code.Unbox:
          return 1;
        case Code.Stobj:
          return 2;
        case Code.Ldobj:
          return 1;
        case Code.Sizeof:
          return 0;
        case Code.Mkrefany:
          return 1;
        default:
          throw new NotImplementedException("OpCode '" + OpCode + "' not implemented! Encountered in method " + aMethod.ToString());
      }
    }

    public override int GetNumberOfStackPushes(MethodBase aMethod)
    {
      switch (OpCode)
      {
        case Code.Initobj:
          return 0;
        case Code.Ldelema:
          return 1;
        case Code.Newarr:
          return 1;
        case Code.Box:
          return 1;
        case Code.Stelem:
          return 0;
        case Code.Ldelem:
          return 1;
        case Code.Isinst:
          return 1;
        case Code.Castclass:
          return 1;
        case Code.Constrained:
          return 0;
        case Code.Unbox_Any:
          return 1;
        case Code.Unbox:
          return 1;
        case Code.Stobj:
          return 0;
        case Code.Ldobj:
          return 1;
        case Code.Sizeof:
          return 1;
        case Code.Mkrefany:
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
        case Code.Initobj:
          return;
        case Code.Ldobj:
          StackPushTypes[0] = Value;
          break;
        case Code.Ldelema:
          StackPopTypes[1] = Value.MakeArrayType();
          StackPushTypes[0] = Base.VoidStar;
          return;
        case Code.Box:
          if (Value.IsValueType)
          {
            StackPushTypes[0] = Base.Box.MakeGenericType(Value);
          }
          else
          {
            StackPushTypes[0] = Value;
          }
          return;
        case Code.Unbox_Any:
          StackPushTypes[0] = Value;
          return;
        case Code.Unbox:
          StackPushTypes[0] = Value;
          return;
        case Code.Newarr:
          StackPushTypes[0] = Value.MakeArrayType();
          return;
        case Code.Stelem:
          StackPopTypes[0] = Value;
          StackPopTypes[2] = Value.MakeArrayType();
          return;
        case Code.Ldelem:
          StackPopTypes[1] = Value.MakeArrayType();
          StackPushTypes[0] = Value;
          return;
        case Code.Isinst:
          StackPopTypes[0] = BaseTypes.Object;
          if (Value.IsGenericType && Value.GetGenericTypeDefinition() == Base.Nullable)
          {
            StackPushTypes[0] = Base.Box.MakeGenericType(Value.GetGenericArguments()[0]);
          }
          else if (Value.IsValueType)
          {
            StackPushTypes[0] = Base.Box.MakeGenericType(Value);
          }
          else
          {
            StackPushTypes[0] = Value;
          }
          return;
        case Code.Castclass:
          if (Value.IsGenericType && Value.GetGenericTypeDefinition() == Base.Nullable)
          {
            StackPushTypes[0] = Base.Box.MakeGenericType(Value.GetGenericArguments()[0]);
          }
          else if (Value.IsValueType)
          {
            StackPushTypes[0] = Base.Box.MakeGenericType(Value);
          }
          else
          {
            StackPushTypes[0] = Value;
          }
          return;
        case Code.Sizeof:
          StackPushTypes[0] = BaseTypes.UInt32;
          return;
        default:
          break;
      }
    }

    public override void DoInterpretStackTypes() { }
  }
}
