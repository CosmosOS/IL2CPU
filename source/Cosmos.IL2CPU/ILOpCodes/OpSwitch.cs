﻿using System;
using System.Collections.Generic;
using System.Linq;

using IL2CPU.Reflection;

namespace Cosmos.IL2CPU.ILOpCodes {
  public class OpSwitch : ILOpCode {
    public IReadOnlyList<int> BranchLocations { get; }

    public OpSwitch(Code aOpCode, int aPos, int aNextPos, IReadOnlyList<int> aBranchLocations, ExceptionBlock aCurrentExceptionRegion)
      : base(aOpCode, aPos, aNextPos, aCurrentExceptionRegion) {
      BranchLocations = aBranchLocations;
    }

    public override int GetNumberOfStackPops(MethodInfo aMethod)
    {
      switch (OpCode)
      {
        case Code.Switch:
          return 1;
        default:
          throw new NotImplementedException("OpCode '" + OpCode + "' not implemented!");
      }
    }

    public override int GetNumberOfStackPushes(MethodInfo aMethod)
    {
      switch (OpCode)
      {
        case Code.Switch:
          return 0;
        default:
          throw new NotImplementedException("OpCode '" + OpCode + "' not implemented!");
      }
    }

    protected override void DoInitStackAnalysis(MethodInfo aMethod)
    {
      base.DoInitStackAnalysis(aMethod);

      switch (OpCode)
      {
        case Code.Switch:
          //StackPopTypes[0] = typeof(uint);
          break;
        default:
          break;
      }
    }

    /// <summary>
    /// Based on updated StackPopTypes, try to update
    /// </summary>
    protected override void DoInterpretStackTypes(ref bool aSituationChanged)
    {
      base.DoInterpretStackTypes(ref aSituationChanged);
      // no switch necessary, there's only 1 instruction using this type.

      if (StackPopTypes[0] == null)
      {
        return;
      }

      if (StackPopTypes[0] == typeof(int) ||
          StackPopTypes[0] == typeof(uint) ||
          StackPopTypes[0] == typeof(short) ||
          StackPopTypes[0] == typeof(ushort) ||
          StackPopTypes[0] == typeof(byte))
      {
        return;
      }
      throw new Exception("Wrong type: " + StackPopTypes[0].FullName);
    }

    protected override void DoInterpretNextInstructionStackTypes(IDictionary<int, ILOpCode> aOpCodes, Stack<TypeInfo> aStack, ref bool aSituationChanged, int aMaxRecursionDepth)
    {
      foreach (var xTarget in BranchLocations)
      {
        base.InterpretInstruction(xTarget, aOpCodes, new Stack<TypeInfo>(aStack.Reverse()), ref aSituationChanged, aMaxRecursionDepth);
      }
      base.DoInterpretNextInstructionStackTypes(aOpCodes, new Stack<TypeInfo>(aStack.Reverse()), ref aSituationChanged, aMaxRecursionDepth);
    }
  }
}
