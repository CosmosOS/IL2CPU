using System;

using IL2CPU.Reflection;
using static Cosmos.IL2CPU.TypeRefHelper;

namespace Cosmos.IL2CPU.ILOpCodes
{
  public class OpType : ILOpCode
  {
    public TypeInfo Value { get; }

    public OpType(Code aOpCode, int aPos, int aNextPos, TypeInfo aValue, ExceptionBlock aCurrentExceptionRegion)
      : base(aOpCode, aPos, aNextPos, aCurrentExceptionRegion)
    {
      Value = aValue;
    }

    public override int GetNumberOfStackPops(MethodInfo aMethod)
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

    public override int GetNumberOfStackPushes(MethodInfo aMethod)
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

    protected override void DoInitStackAnalysis(MethodInfo aMethod)
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
          StackPushTypes[0] = TypeOf(BclType.Void).MakePointerType();
          return;
        case Code.Box:
          if (Value.IsValueType)
          {
            StackPushTypes[0] = TypeOf(typeof(Box<>)).MakeGenericType(Value);
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
          StackPopTypes[0] = TypeOf(BclType.Object);
          if (Value.IsGenericType && Value.GetGenericTypeDefinition() == typeof(Nullable<>))
          {
            StackPushTypes[0] = TypeOf(typeof(Box<>)).MakeGenericType(Value.GenericArguments[0]);
          }
          else if (Value.IsValueType)
          {
            StackPushTypes[0] = TypeOf(typeof(Box<>)).MakeGenericType(Value);
          }
          else
          {
            StackPushTypes[0] = Value;
          }
          return;
        case Code.Castclass:
          if (Value.IsGenericType && Value.GetGenericTypeDefinition() == TypeOf(typeof(Nullable<>)))
          {
            StackPushTypes[0] = TypeOf(typeof(Box<>)).MakeGenericType(Value.GenericArguments[0]);
          }
          else if (Value.IsValueType)
          {
            StackPushTypes[0] = TypeOf(typeof(Box<>)).MakeGenericType(Value);
          }
          else
          {
            StackPushTypes[0] = Value;
          }
          return;
        case Code.Sizeof:
          StackPushTypes[0] = TypeOf(BclType.UInt32);
          return;
        default:
          break;
      }
    }
  }
}
