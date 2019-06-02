
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace Cosmos.IL2CPU.ILOpCodes
{
  public class OpBranch : ILOpCode
  {
    public int Value { get; }

    public OpBranch(Code aOpCode, int aPos, int aNextPos, int aValue, _ExceptionRegionInfo aCurrentExceptionRegion)
      : base(aOpCode, aPos, aNextPos, aCurrentExceptionRegion)
    {
      Value = aValue;
    }

    public override int GetNumberOfStackPops(MethodBase aMethod)
    {
      switch (OpCode)
      {
        case Code.Leave:
        case Code.Br:
          return 0;
        case Code.Brtrue:
          return 1;
        case Code.Brfalse:
          return 1;
        case Code.Beq:
        case Code.Ble:
        case Code.Ble_Un:
        case Code.Bne_Un:
        case Code.Bge:
        case Code.Bge_Un:
        case Code.Bgt:
        case Code.Bgt_Un:
        case Code.Blt:
        case Code.Blt_Un:
          return 2;
        default:
          throw new NotImplementedException("OpCode '" + OpCode + "' not implemented!");
      }
    }

    public override int GetNumberOfStackPushes(MethodBase aMethod)
    {
      switch (OpCode)
      {
        case Code.Leave:
        case Code.Br:
          return 0;
        case Code.Brtrue:
          return 0;
        case Code.Brfalse:
          return 0;
        case Code.Beq:
        case Code.Ble:
        case Code.Ble_Un:
        case Code.Bne_Un:
        case Code.Bge:
        case Code.Bge_Un:
        case Code.Bgt:
        case Code.Bgt_Un:
        case Code.Blt:
        case Code.Blt_Un:
          return 0;
        default:
          throw new NotImplementedException("OpCode '" + OpCode + "' not implemented!");
      }
    }

    protected override void DoInitStackAnalysis(MethodBase aMethod)
    {
      base.DoInitStackAnalysis(aMethod);

      switch (OpCode)
      {
        default:
          break;
      }
    }

    protected override void DoInterpretStackTypes(ref bool aSituationChanged)
    {
      base.DoInterpretStackTypes(ref aSituationChanged);
      // this method is supposed to deduct push types from pop types. Branch ops don't push, but we want to do checks here,
      // to help verify other code is right
      switch (OpCode)
      {
        case Code.Brtrue:
        case Code.Brfalse:
          // check pop types according to ECMA 335
          var xPopType = StackPopTypes[0];
          if (xPopType == null)
          {
            return;
          }
          if (ILOp.IsIntegralType(xPopType))
          {
            return;
          }
          if (xPopType.IsClass)
          {
            return;
          }
          if (xPopType.IsInterface)
          {
            return;
          }
          // ECMA apparently sees a boolean on the stack as a native int. We push as boolean, so acccept that as well
          if (xPopType == typeof(bool))
          {
            return;
          }

          throw new Exception("Invalid type in PopTypes! (Type = '" + xPopType.AssemblyQualifiedName + "')");
        case Code.Br:
        case Code.Leave:
          return;
        case Code.Blt:
        case Code.Ble:
        case Code.Beq:
        case Code.Bge:
        case Code.Bgt:
        case Code.Bge_Un:
        case Code.Blt_Un:
        case Code.Ble_Un:
        case Code.Bne_Un:
        case Code.Bgt_Un:
          var xValue1 = StackPopTypes[0];
          var xValue2 = StackPopTypes[1];
          if (xValue1 == null || xValue2 == null)
          {
            return;
          }
          if (ILOp.IsIntegralTypeOrPointer(xValue1) && ILOp.IsIntegralTypeOrPointer(xValue2))
          {
            return;
          }
          if (xValue1 == typeof(float) && xValue2 == typeof(float))
          {
            return;
          }
          if (xValue1 == typeof(double) && xValue2 == typeof(double))
          {
            return;
          }
          if ((xValue1 == typeof(int) && xValue2 == typeof(bool))
            || (xValue1 == typeof(bool) && xValue2 == typeof(int)))
          {
            return;
          }

          var xType1 = Type.GetTypeCode(xValue1.IsEnum ? Enum.GetUnderlyingType(xValue1) : xValue1);
          var xType2 = Type.GetTypeCode(xValue2.IsEnum ? Enum.GetUnderlyingType(xValue2) : xValue2);

          if ((xType1 == TypeCode.Boolean)
            && (xType2 == TypeCode.Boolean))
          {
            return;
          }
          if ((xType1 == TypeCode.Char)
            && (xType2 == TypeCode.Char))
          {
            return;
          }
          if ((xType1 == TypeCode.SByte || xType1 == TypeCode.Byte)
            && (xType2 == TypeCode.Byte || xType2 == TypeCode.SByte))
          {
            return;
          }
          if ((xType1 == TypeCode.Int16 || xType1 == TypeCode.UInt16)
            && (xType2 == TypeCode.UInt16 || xType2 == TypeCode.Int16))
          {
            return;
          }
          //The TypeCode for IntPtr is Object
          //The TypeCode for UIntPtr is Object
          //The TypeCode for Byte* is Object
          if ((xType1 == TypeCode.Int32 || xType1 == TypeCode.UInt32 || xType1 == TypeCode.Object)
            && (xType2 == TypeCode.UInt32 || xType2 == TypeCode.Int32 || xType2 == TypeCode.Object))
          {
            return;
          }
          if ((xType1 == TypeCode.Int64 || xType1 == TypeCode.UInt64)
            && (xType2 == TypeCode.UInt64 || xType2 == TypeCode.Int64))
          {
            return;
          }
          if ((xType1 == TypeCode.DateTime)
            && (xType2 == TypeCode.DateTime))
          {
            return;
          }
          if ((xValue1.IsClass || xValue1.IsInterface)
            && (xValue2.IsClass || xValue2.IsInterface))
          {
            return;
          }

          throw new Exception(String.Format("Comparing types '{0}' and '{1}' not supported!", xValue1.AssemblyQualifiedName, xValue2.AssemblyQualifiedName));
        default:
          throw new NotImplementedException("Checks for opcode " + OpCode + " not implemented!");
      }
    }

    protected override void DoInterpretNextInstructionStackTypes(IDictionary<int, ILOpCode> aOpCodes, Stack<Type> aStack, ref bool aSituationChanged, int aMaxRecursionDepth)
    {
      switch (OpCode)
      {
        case Code.Brtrue:
        case Code.Brfalse:
        case Code.Blt:
        case Code.Blt_Un:
        case Code.Ble:
        case Code.Ble_Un:
        case Code.Bgt:
        case Code.Bgt_Un:
        case Code.Bge:
        case Code.Bge_Un:
        case Code.Beq:
        case Code.Bne_Un:
        case Code.Br:
          InterpretInstructionIfNotYetProcessed(Value, aOpCodes, new Stack<Type>(aStack.Reverse()), ref aSituationChanged, aMaxRecursionDepth);
          base.DoInterpretNextInstructionStackTypesIfNotYetProcessed(aOpCodes, new Stack<Type>(aStack.Reverse()), ref aSituationChanged, aMaxRecursionDepth);
          break;
        case Code.Leave:
          InterpretInstructionIfNotYetProcessed(Value, aOpCodes, new Stack<Type>(aStack.Reverse()), ref aSituationChanged, aMaxRecursionDepth);
          base.DoInterpretNextInstructionStackTypesIfNotYetProcessed(aOpCodes, new Stack<Type>(aStack.Reverse()), ref aSituationChanged, aMaxRecursionDepth);

          break;
        default:
          throw new NotImplementedException("OpCode " + OpCode);
      }
    }
  }
}
