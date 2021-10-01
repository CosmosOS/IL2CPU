using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cosmos.IL2CPU.Extensions;
using static IL2CPU.Reflection.BaseTypeSystem;

namespace Cosmos.IL2CPU.ILOpCodes
{
  public class OpNone : ILOpCode
  {

    public OpNone(Code aOpCode, int aPos, int aNextPos, _ExceptionRegionInfo aCurrentExceptionRegion)
      : base(aOpCode, aPos, aNextPos, aCurrentExceptionRegion)
    {
    }

    public override int GetNumberOfStackPops(MethodBase aMethod)
    {
      switch (OpCode)
      {
        case Code.Pop:
          return 1;
        case Code.Nop:
          return 0;
        case Code.Ret:
          var methodInfo = aMethod as MethodInfo;
          if (methodInfo != null && methodInfo.ReturnType != BaseTypes.Void)
          {
            return 1;
          }
          else
          {
            return 0;
          }
        case Code.Conv_I:
        case Code.Conv_I1:
        case Code.Conv_I2:
        case Code.Conv_I4:
        case Code.Conv_I8:
        case Code.Conv_U:
        case Code.Conv_U1:
        case Code.Conv_U2:
        case Code.Conv_U4:
        case Code.Conv_U8:
        case Code.Conv_R4:
        case Code.Conv_R8:
        case Code.Conv_R_Un:
        case Code.Conv_Ovf_I:
        case Code.Conv_Ovf_I1:
        case Code.Conv_Ovf_I1_Un:
        case Code.Conv_Ovf_I2:
        case Code.Conv_Ovf_I2_Un:
        case Code.Conv_Ovf_I4:
        case Code.Conv_Ovf_I4_Un:
        case Code.Conv_Ovf_I8:
        case Code.Conv_Ovf_I8_Un:
        case Code.Conv_Ovf_I_Un:
        case Code.Conv_Ovf_U:
        case Code.Conv_Ovf_U1:
        case Code.Conv_Ovf_U1_Un:
        case Code.Conv_Ovf_U2:
        case Code.Conv_Ovf_U2_Un:
        case Code.Conv_Ovf_U4:
        case Code.Conv_Ovf_U4_Un:
        case Code.Conv_Ovf_U8:
        case Code.Conv_Ovf_U8_Un:
        case Code.Conv_Ovf_U_Un:
          return 1;
        case Code.Add:
        case Code.Add_Ovf:
        case Code.Add_Ovf_Un:
        case Code.Mul:
        case Code.Mul_Ovf:
        case Code.Mul_Ovf_Un:
        case Code.Div:
        case Code.Div_Un:
        case Code.Sub:
        case Code.Sub_Ovf:
        case Code.Sub_Ovf_Un:
        case Code.Rem:
        case Code.Rem_Un:
        case Code.Xor:
          return 2;
        case Code.Ldind_I:
        case Code.Ldind_I1:
        case Code.Ldind_I2:
        case Code.Ldind_I4:
        case Code.Ldind_I8:
        case Code.Ldind_U1:
        case Code.Ldind_U2:
        case Code.Ldind_U4:
        case Code.Ldind_R4:
        case Code.Ldind_R8:
        case Code.Ldind_Ref:
          return 1;
        case Code.Stind_I:
        case Code.Stind_I1:
        case Code.Stind_I2:
        case Code.Stind_I4:
        case Code.Stind_I8:
        case Code.Stind_R4:
        case Code.Stind_R8:
        case Code.Stind_Ref:
          return 2;
        case Code.Clt:
          return 2;
        case Code.Clt_Un:
          return 2;
        case Code.Cgt:
          return 2;
        case Code.Cgt_Un:
          return 2;
        case Code.Ceq:
          return 2;
        case Code.Throw:
          return 1;
        case Code.Rethrow:
          return 0;
        case Code.Or:
        case Code.And:
          return 2;
        case Code.Not:
          return 1;
        case Code.Stelem_Ref:
        case Code.Stelem_I:
        case Code.Stelem_I1:
        case Code.Stelem_I2:
        case Code.Stelem_I4:
        case Code.Stelem_I8:
        case Code.Stelem_R4:
        case Code.Stelem_R8:
          return 3;
        case Code.Shr:
        case Code.Shr_Un:
        case Code.Shl:
          return 2;
        case Code.Neg:
          return 1;
        case Code.Localloc:
          return 1;
        case Code.Ldlen:
          return 1;
        case Code.Ldelem:
        case Code.Ldelem_Ref:
        case Code.Ldelem_I:
        case Code.Ldelem_I1:
        case Code.Ldelem_I2:
        case Code.Ldelem_I4:
        case Code.Ldelem_I8:
        case Code.Ldelem_U1:
        case Code.Ldelem_U2:
        case Code.Ldelem_U4:
        case Code.Ldelem_R4:
        case Code.Ldelem_R8:
          return 2;
        case Code.Ldnull:
          return 0;
        case Code.Dup:
          return 1;
        case Code.Volatile:
          return 0;
        case Code.Endfinally:
        case Code.Endfilter:
          return 0;
        case Code.Refanytype:
          return 1;
        case Code.Initblk:
          return 3;
        default:
          throw new NotImplementedException("OpCode '" + OpCode + "' not implemented!");
      }
    }

    public override int GetNumberOfStackPushes(MethodBase aMethod)
    {
      switch (OpCode)
      {
        case Code.Pop:
          return 0;
        case Code.Ret:
          return 0;
        case Code.Nop:
          return 0;
        case Code.Conv_I:
        case Code.Conv_I1:
        case Code.Conv_I2:
        case Code.Conv_I4:
        case Code.Conv_I8:
        case Code.Conv_U:
        case Code.Conv_U1:
        case Code.Conv_U2:
        case Code.Conv_U4:
        case Code.Conv_U8:
        case Code.Conv_R4:
        case Code.Conv_R8:
        case Code.Conv_R_Un:
        case Code.Conv_Ovf_I:
        case Code.Conv_Ovf_I1:
        case Code.Conv_Ovf_I1_Un:
        case Code.Conv_Ovf_I2:
        case Code.Conv_Ovf_I2_Un:
        case Code.Conv_Ovf_I4:
        case Code.Conv_Ovf_I4_Un:
        case Code.Conv_Ovf_I8:
        case Code.Conv_Ovf_I8_Un:
        case Code.Conv_Ovf_I_Un:
        case Code.Conv_Ovf_U:
        case Code.Conv_Ovf_U1:
        case Code.Conv_Ovf_U1_Un:
        case Code.Conv_Ovf_U2:
        case Code.Conv_Ovf_U2_Un:
        case Code.Conv_Ovf_U4:
        case Code.Conv_Ovf_U4_Un:
        case Code.Conv_Ovf_U8:
        case Code.Conv_Ovf_U8_Un:
        case Code.Conv_Ovf_U_Un:
          return 1;
        case Code.Add:
        case Code.Add_Ovf:
        case Code.Add_Ovf_Un:
        case Code.Mul:
        case Code.Mul_Ovf:
        case Code.Mul_Ovf_Un:
        case Code.Div:
        case Code.Div_Un:
        case Code.Sub:
        case Code.Sub_Ovf:
        case Code.Sub_Ovf_Un:
        case Code.Rem:
        case Code.Rem_Un:
        case Code.Xor:
          return 1;
        case Code.Ldind_I:
        case Code.Ldind_I1:
        case Code.Ldind_I2:
        case Code.Ldind_I4:
        case Code.Ldind_I8:
        case Code.Ldind_U1:
        case Code.Ldind_U2:
        case Code.Ldind_U4:
        case Code.Ldind_R4:
        case Code.Ldind_R8:
        case Code.Ldind_Ref:
          return 1;
        case Code.Stind_I:
        case Code.Stind_I1:
        case Code.Stind_I2:
        case Code.Stind_I4:
        case Code.Stind_I8:
        case Code.Stind_R4:
        case Code.Stind_R8:
        case Code.Stind_Ref:
          return 0;
        case Code.Clt:
          return 1;
        case Code.Clt_Un:
          return 1;
        case Code.Cgt:
          return 1;
        case Code.Cgt_Un:
          return 1;
        case Code.Ceq:
          return 1;
        case Code.Throw:
        case Code.Rethrow:
          return 0;
        case Code.Or:
        case Code.And:
        case Code.Not:
          return 1;
        case Code.Stelem_I:
        case Code.Stelem_I1:
        case Code.Stelem_I2:
        case Code.Stelem_I4:
        case Code.Stelem_I8:
        case Code.Stelem_R4:
        case Code.Stelem_R8:
        case Code.Stelem_Ref:
          return 0;
        case Code.Shr:
        case Code.Shr_Un:
        case Code.Shl:
          return 1;
        case Code.Neg:
          return 1;
        case Code.Localloc:
          return 1;
        case Code.Ldlen:
          return 1;
        case Code.Ldelem:
        case Code.Ldelem_Ref:
        case Code.Ldelem_I:
        case Code.Ldelem_I1:
        case Code.Ldelem_I2:
        case Code.Ldelem_I4:
        case Code.Ldelem_I8:
        case Code.Ldelem_U1:
        case Code.Ldelem_U2:
        case Code.Ldelem_U4:
        case Code.Ldelem_R4:
        case Code.Ldelem_R8:
          return 1;
        case Code.Ldnull:
          return 1;
        case Code.Dup:
          return 2;
        case Code.Volatile:
          return 0;
        case Code.Endfinally:
        case Code.Endfilter:
          return 0;
        case Code.Refanytype:
          return 1;
        case Code.Initblk:
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
        case Code.Ldind_U1:
          StackPushTypes[0] = BaseTypes.Byte;
          return;

        case Code.Ldind_U2:
          StackPushTypes[0] = BaseTypes.UInt16;
          return;

        case Code.Ldind_U4:
          StackPushTypes[0] = BaseTypes.UInt32;
          return;

        case Code.Ldind_R4:
          StackPushTypes[0] = BaseTypes.Single;
          return;

        case Code.Ldind_R8:
          StackPushTypes[0] = BaseTypes.Double;
          return;

        case Code.Conv_I:
          StackPushTypes[0] = BaseTypes.IntPtr;
          break;

        case Code.Conv_I1:
          StackPushTypes[0] = BaseTypes.SByte;
          break;

        case Code.Conv_I2:
          StackPushTypes[0] = BaseTypes.Int16;
          break;

        case Code.Conv_I4:
          StackPushTypes[0] = BaseTypes.Int32;
          break;

        case Code.Conv_I8:
          StackPushTypes[0] = BaseTypes.Int64;
          break;

        case Code.Conv_U:
          StackPushTypes[0] = BaseTypes.UIntPtr;
          break;

        case Code.Conv_U1:
          StackPushTypes[0] = BaseTypes.Byte;
          break;

        case Code.Conv_U2:
          StackPushTypes[0] = BaseTypes.UInt16;
          break;

        case Code.Conv_U4:
          StackPushTypes[0] = BaseTypes.UInt32;
          break;

        case Code.Conv_U8:
          StackPushTypes[0] = BaseTypes.UInt64;
          break;

        case Code.Conv_R4:
          StackPushTypes[0] = BaseTypes.Single;
          break;

        case Code.Conv_R8:
          StackPushTypes[0] = BaseTypes.Double;
          break;
        case Code.Conv_Ovf_I:
          StackPushTypes[0] = BaseTypes.IntPtr;
          break;
        case Code.Conv_Ovf_I1:
          StackPushTypes[0] = BaseTypes.SByte;
          break;
        case Code.Conv_Ovf_I1_Un:
          StackPushTypes[0] = BaseTypes.SByte;
          break;
        case Code.Conv_Ovf_I2:
          StackPushTypes[0] = BaseTypes.Int16;
          break;
        case Code.Conv_Ovf_I2_Un:
          StackPushTypes[0] = BaseTypes.Int16;
          break;
        case Code.Conv_Ovf_I4:
          StackPushTypes[0] = BaseTypes.Int32;
          break;
        case Code.Conv_Ovf_I4_Un:
          StackPushTypes[0] = BaseTypes.Int32;
          break;
        case Code.Conv_Ovf_I8:
          StackPushTypes[0] = BaseTypes.Int64;
          break;
        case Code.Conv_Ovf_I8_Un:
          StackPushTypes[0] = BaseTypes.Int64;
          break;
        case Code.Conv_Ovf_I_Un:
          StackPushTypes[0] = BaseTypes.IntPtr;
          break;
        case Code.Conv_Ovf_U:
          StackPushTypes[0] = BaseTypes.UIntPtr;
          break;
        case Code.Conv_Ovf_U1:
          StackPushTypes[0] = BaseTypes.Byte;
          break;
        case Code.Conv_Ovf_U1_Un:
          StackPushTypes[0] = BaseTypes.Byte;
          break;
        case Code.Conv_Ovf_U2:
          StackPushTypes[0] = BaseTypes.UInt16;
          break;
        case Code.Conv_Ovf_U2_Un:
          StackPushTypes[0] = BaseTypes.UInt16;
          break;
        case Code.Conv_Ovf_U4:
          StackPushTypes[0] = BaseTypes.UInt32;
          break;
        case Code.Conv_Ovf_U4_Un:
          StackPushTypes[0] = BaseTypes.UInt32;
          break;
        case Code.Conv_Ovf_U8:
          StackPushTypes[0] = BaseTypes.UInt64;
          break;
        case Code.Conv_Ovf_U8_Un:
          StackPushTypes[0] = BaseTypes.UInt64;
          break;
        case Code.Conv_Ovf_U_Un:
          StackPushTypes[0] = BaseTypes.UIntPtr;
          break;

        case Code.Clt:
          StackPushTypes[0] = BaseTypes.Int32;
          return;
        case Code.Clt_Un:
          StackPushTypes[0] = BaseTypes.Int32;
          return;
        case Code.Cgt:
          StackPushTypes[0] = BaseTypes.Int32;
          return;
        case Code.Cgt_Un:
          StackPushTypes[0] = BaseTypes.Int32;
          return;
        case Code.Ceq:
          StackPushTypes[0] = BaseTypes.Int32;
          return;
        case Code.Throw:
          StackPopTypes[0] = BaseTypes.Object;
          return;
        case Code.Ldlen:
          StackPushTypes[0] = BaseTypes.UIntPtr;
          return;

        case Code.Ldelem_I:
          StackPushTypes[0] = BaseTypes.IntPtr;
          return;
        case Code.Ldelem_I1:
          StackPushTypes[0] = BaseTypes.SByte;
          return;
        case Code.Ldelem_I2:
          StackPushTypes[0] = BaseTypes.Int16;
          return;
        case Code.Ldelem_I4:
          StackPushTypes[0] = BaseTypes.Int32;
          return;
        case Code.Ldelem_I8:
          StackPushTypes[0] = BaseTypes.Int64;
          return;
        case Code.Ldelem_U1:
          StackPushTypes[0] = BaseTypes.Byte;
          return;
        case Code.Ldelem_U2:
          StackPushTypes[0] = BaseTypes.UInt16;
          return;
        case Code.Ldelem_U4:
          StackPushTypes[0] = BaseTypes.UInt32;
          return;
        case Code.Ldelem_R4:
          StackPushTypes[0] = BaseTypes.Single;
          return;
        case Code.Ldelem_R8:
          StackPushTypes[0] = BaseTypes.Double;
          return;
        case Code.Ldnull:
          StackPushTypes[0] = Base.NullRef;
          return;
        case Code.Ldind_I:
          StackPushTypes[0] = BaseTypes.IntPtr;
          return;

        case Code.Ldind_I1:
          StackPushTypes[0] = BaseTypes.SByte;
          return;

        case Code.Ldind_I2:
          StackPushTypes[0] = BaseTypes.Int16;
          return;

        case Code.Ldind_I4:
          StackPushTypes[0] = BaseTypes.Int32;
          return;

        case Code.Ldind_I8:
          StackPushTypes[0] = BaseTypes.Int64;
          return;

        case Code.Stelem_I4:
          StackPopTypes[0] = BaseTypes.Int32;
          return;

        case Code.Stelem_I8:
          StackPopTypes[0] = BaseTypes.Int64;
          return;
        case Code.Conv_R_Un:
          StackPushTypes[0] = BaseTypes.Double;
          return;
      }
    }

    public override void DoInterpretStackTypes()
    {
      switch (OpCode)
      {
        case Code.Add:
        case Code.Add_Ovf:
        case Code.Add_Ovf_Un:
        case Code.Mul:
        case Code.Mul_Ovf:
        case Code.Mul_Ovf_Un:
        case Code.Div:
        case Code.Div_Un:
        case Code.Sub:
        case Code.Sub_Ovf:
        case Code.Sub_Ovf_Un:
        case Code.Rem:
        case Code.Rem_Un:
        case Code.Xor:
        case Code.And:
        case Code.Or:
          if (StackPushTypes[0] != null)
          {
            return;
          }
          if (!StackPopTypes.Contains(null))
          {
            // PopTypes set, but PushType not yet, so fill it.
            if(StackPopTypes[0] == StackPopTypes[1])
            {
              StackPushTypes[0] = StackPopTypes[0];
              return;
            }

            if (ILOp.IsIntegerBasedType(StackPopTypes[0]) && ILOp.IsIntegerBasedType(StackPopTypes[1]))
            {
              StackPushTypes[0] = BaseTypes.Int32;
              return;
            }

            if (ILOp.IsLongBasedType(StackPopTypes[0]) && ILOp.IsLongBasedType(StackPopTypes[1]))
            {
              StackPushTypes[0] = BaseTypes.Int64;
              return;
            }

            if (ILOp.IsPointer(StackPopTypes[0]) && ILOp.IsPointer(StackPopTypes[1]))
            {
              StackPushTypes[0] = Base.UintStar;
              return;
            }

            if ((ILOp.IsPointer(StackPopTypes[0]) && ILOp.IsIntegerBasedType(StackPopTypes[1]))
                 || (ILOp.IsIntegerBasedType(StackPopTypes[0]) && ILOp.IsPointer(StackPopTypes[1])))
            {
              StackPushTypes[0] = Base.UintStar;
              return;
            }
            
            throw new NotImplementedException(string.Format("{0} on types '{1}' and '{2}' {3} not yet implemented!", OpCode, StackPopTypes[0], StackPopTypes[1], StackPopTypes[1].IsByRef));
          }
          break;
        case Code.Localloc:
          StackPushTypes[0] = Base.VoidStar;
          return;
        case Code.Stelem_I2:
          var xTypeValue = StackPopTypes[0];
          if (xTypeValue == null)
          {
            return;
          }

          if (xTypeValue == BaseTypes.Byte
            || xTypeValue == BaseTypes.Char
            || xTypeValue == BaseTypes.Int16
            || xTypeValue == BaseTypes.UInt16
            || xTypeValue == BaseTypes.Int32)
          {
            return;
          }
          throw new NotImplementedException(String.Format("Stelem_I2 storing type '{0}' is not implemented!", xTypeValue));
        case Code.Shl:
        case Code.Shr:
        case Code.Shr_Un:
          xTypeValue = StackPopTypes[1];
          var xTypeShift = StackPopTypes[0];
          if (xTypeValue == null || xTypeShift == null)
          {
            return;
          }
          if (xTypeValue == BaseTypes.Int32 && xTypeShift == BaseTypes.Int32)
          {
            StackPushTypes[0] = BaseTypes.Int32;
            return;
          }
          if (xTypeValue == BaseTypes.Byte && xTypeShift == BaseTypes.Int32)
          {
            StackPushTypes[0] = BaseTypes.Int32;
            return;
          }
          if (xTypeValue == BaseTypes.Int64 && xTypeShift == BaseTypes.Int32)
          {
            StackPushTypes[0] = BaseTypes.Int64;
            return;
          }
          if (xTypeValue == BaseTypes.IntPtr && xTypeShift == BaseTypes.Int32)
          {
            StackPushTypes[0] = BaseTypes.Int32;
            return;
          }
          if (xTypeValue == BaseTypes.Int32 && xTypeShift == BaseTypes.IntPtr)
          {
            StackPushTypes[0] = BaseTypes.Int32;
            return;
          }
          if (xTypeValue == BaseTypes.UInt16 && xTypeShift == BaseTypes.Int32)
          {
            StackPushTypes[0] = BaseTypes.Int32;
            return;
          }
          if (xTypeValue == BaseTypes.Char && xTypeShift == BaseTypes.Int32)
          {
            StackPushTypes[0] = BaseTypes.Int32;
            return;
          }
          if (xTypeValue == BaseTypes.UInt32 && xTypeShift == BaseTypes.Int32)
          {
            StackPushTypes[0] = BaseTypes.Int32;
            return;
          }
          if (xTypeValue == BaseTypes.Int64 && xTypeShift == BaseTypes.IntPtr)
          {
            StackPushTypes[0] = BaseTypes.Int64;
            return;
          }
          if (xTypeValue == BaseTypes.IntPtr && xTypeShift == BaseTypes.IntPtr)
          {
            StackPushTypes[0] = BaseTypes.IntPtr;
            return;
          }
          if (xTypeValue == BaseTypes.IntPtr && xTypeShift == BaseTypes.IntPtr)
          {
            StackPushTypes[0] = BaseTypes.IntPtr;
            return;
          }
          if (xTypeValue == BaseTypes.UInt64 && xTypeShift == BaseTypes.Int32)
          {
            StackPushTypes[0] = BaseTypes.UInt64;
            return;
          }
          if (xTypeValue == BaseTypes.SByte && xTypeShift == BaseTypes.Int32)
          {
            StackPushTypes[0] = BaseTypes.Int32;
            return;
          }
          if (xTypeValue == BaseTypes.Int16 && xTypeShift == BaseTypes.Int32)
          {
            StackPushTypes[0] = BaseTypes.Int32;
            return;
          }
          if (xTypeValue == BaseTypes.UIntPtr && xTypeShift == BaseTypes.Int32)
          {
            StackPushTypes[0] = BaseTypes.UIntPtr;
            return;
          }
          if (xTypeValue == Base.CharStar && xTypeShift == BaseTypes.Int32)
          {
            StackPushTypes[0] = Base.CharStar;
            return;
          }
          throw new NotImplementedException(String.Format("{0} with types {1} and {2} is not implemented!", OpCode, xTypeValue.FullName, xTypeShift.FullName));
        case Code.Ldelem_Ref:
          if (StackPushTypes[0] != null)
          {
            return;
          }
          var xTypeArray = StackPopTypes[1];
          if (xTypeArray == null)
          {
            return;
          }
          if (!xTypeArray.IsArray)
          {
            throw new Exception("Ldelem Array type is not an array (Actual = " + xTypeArray.FullName + ")");
          }
          StackPushTypes[0] = xTypeArray.GetElementType();
          break;
        case Code.Not:
        case Code.Neg:
          if (StackPushTypes[0] != null)
          {
            return;
          }
          if (StackPopTypes[0] != null)
          {
            StackPushTypes[0] = StackPopTypes[0];
            return;
          }
          break;
        case Code.Dup:
          if (StackPopTypes[0] != null)
          {
            StackPushTypes[0] = StackPopTypes[0];
            StackPushTypes[1] = StackPopTypes[0];
            return;
          }
          return;
        case Code.Stind_I1:
          if (!ILOp.IsIntegerBasedType(StackPopTypes[0]))
          {
            throw new Exception("Wrong value type: " + StackPopTypes[0].FullName);
          }
          if (!ILOp.IsPointer(StackPopTypes[1]) && StackPopTypes[1] != BaseTypes.Int32) // can either be pointer or native int
          {
            throw new Exception("Wrong Pointer type: " + StackPopTypes[1].FullName);
          }
          break;
        case Code.Stind_I2:
          if (!ILOp.IsIntegerBasedType(StackPopTypes[0]))
          {
            throw new Exception("Wrong value type: " + StackPopTypes[0].FullName);
          }
          if (!ILOp.IsPointer(StackPopTypes[1]) && StackPopTypes[1] != BaseTypes.Int32)
          {
            throw new Exception("Wrong Pointer type: " + StackPopTypes[1].FullName);
          }
          break;
        case Code.Stind_I4:
          if (!ILOp.IsIntegerBasedType(StackPopTypes[0]))
          {
            throw new Exception("Wrong value type: " + StackPopTypes[0].FullName);
          }
          if (!ILOp.IsPointer(StackPopTypes[1]) && StackPopTypes[1] != BaseTypes.Int32)
          {
            throw new Exception("Wrong Pointer type: " + StackPopTypes[1].FullName);
          }
          break;
        case Code.Stind_I8:
          if (!ILOp.IsLongBasedType(StackPopTypes[0]))
          {
            throw new Exception("Wrong value type: " + StackPopTypes[0].FullName);
          }
          if (!ILOp.IsPointer(StackPopTypes[1]) && StackPopTypes[1] != BaseTypes.Int32)
          {
            throw new Exception("Wrong Pointer type: " + StackPopTypes[1].FullName);
          }
          break;
        case Code.Stind_I:
          if (!ILOp.IsIntegralTypeOrPointer(StackPopTypes[0]))
          {
            throw new Exception("Wrong value type: " + StackPopTypes[0].FullName);
          }
          if (!ILOp.IsPointer(StackPopTypes[1]) && StackPopTypes[1] != BaseTypes.Int32)
          {
            throw new Exception("Wrong Pointer type: " + StackPopTypes[1].FullName);
          }
          break;
        case Code.Ldind_Ref:
          if (StackPushTypes[0] != null)
          {
            return;
          }
          if (StackPopTypes[0] == null)
          {
            return;
          }
          if (!StackPopTypes[0].IsByRef && !StackPopTypes[0].IsPointer)
          {
            throw new Exception("Invalid ref type: " + StackPopTypes[0].FullName);
          }
          if (StackPopTypes[0].IsPointer)
          {
            StackPushTypes[0] = BaseTypes.Object;
          }
          else
          {
            StackPushTypes[0] = StackPopTypes[0].GetElementType();
          }
          break;
      }
    }

    public override List<(bool newGroup, int Position)> GetNextOpCodePositions()
    {
      switch (OpCode)
      {
        case Code.Ret:
        case Code.Throw:
          return new List<(bool newGroup, int Position)>();
        default:
          return new List<(bool newGroup, int Position)> { (false, NextPosition) };
      }
    }
  }
}
