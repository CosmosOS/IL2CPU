using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using static IL2CPU.Reflection.BaseTypeSystem;

namespace Cosmos.IL2CPU.ILOpCodes {
  public class OpSwitch : ILOpCode {
    public int[] BranchLocations { get; }

    public OpSwitch(Code aOpCode, int aPos, int aNextPos, int[] aBranchLocations, _ExceptionRegionInfo aCurrentExceptionRegion)
      : base(aOpCode, aPos, aNextPos, aCurrentExceptionRegion) {
      BranchLocations = aBranchLocations;
    }

    public override int GetNumberOfStackPops(MethodBase aMethod)
    {
      switch (OpCode)
      {
        case Code.Switch:
          return 1;
        default:
          throw new NotImplementedException("OpCode '" + OpCode + "' not implemented!");
      }
    }

    public override int GetNumberOfStackPushes(MethodBase aMethod)
    {
      switch (OpCode)
      {
        case Code.Switch:
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
    public override void DoInterpretStackTypes()
    {
      // no switch necessary, there's only 1 instruction using this type.

      if (StackPopTypes[0] == null)
      {
        return;
      }

      if (StackPopTypes[0] == BaseTypes.Int32 ||
          StackPopTypes[0] == BaseTypes.UInt32 ||
          StackPopTypes[0] == BaseTypes.Int16 ||
          StackPopTypes[0] == BaseTypes.UInt16 ||
          StackPopTypes[0] == BaseTypes.Byte)
      {
        return;
      }
      throw new Exception("Wrong type: " + StackPopTypes[0].FullName);
    }

    public override List<(bool newGroup, int Position)> GetNextOpCodePositions()
    {
      var Positions = new List<(bool, int)>();

      foreach (var xTarget in BranchLocations)
      {
        Positions.Add((true, xTarget));
      }

      // switch allows fall through. see ecma-355 I.12.4.2.8.1 Fall Through
      Positions.Add((true, NextPosition));

      return Positions;
    }
  }
}
