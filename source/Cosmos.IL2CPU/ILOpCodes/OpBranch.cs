
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using static IL2CPU.Reflection.BaseTypeSystem;

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


    public override void DoInterpretStackTypes()
    {
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
          if (ILOp.IsIntegerBasedType(xPopType) || ILOp.IsLongBasedType(xPopType))
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
          if (xPopType == BaseTypes.Boolean)
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

          if (ILOp.IsSameValueType(xValue1, xValue2))
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

    public override List<(bool newGroup, int Position)> GetNextOpCodePositions()
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
        case Code.Leave:
          return new List<(bool newGroup, int Position)> { (true, Value), (true, NextPosition) }; // technically we dont need to have the second as true but this means we get the same blocks as in ilspy
        case Code.Br: //An unconditional branch will never not branch, so we dont interpret stack if we didnt branch (as done for other branches)
                      // Otherwise, this can lead to bugs, as the opcode after an unconditional branch is reached via a jump with a different stack, than the one
                      // in the block ending with the unconditional branch before.
                      // Can be reproduced by trying to compile this code: `char c2 = (c <= 'Z' && c >= 'A') ? ((char)(c - 65 + 97)) : c;`
          return new List<(bool newGroup, int Position)> { (true, Value) };
        default:
          throw new NotImplementedException("OpCode " + OpCode);
      }
    }
  }
}
