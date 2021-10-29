using Cosmos.IL2CPU.ILOpCodes;
using System;
using Cosmos.IL2CPU.Extensions;
using XSharp;
using XSharp.Assembler;
using static IL2CPU.Reflection.BaseTypeSystem;

namespace Cosmos.IL2CPU.X86.IL
{
  [OpCode(ILOpCode.Code.Constrained)]
  public class Constrained : ILOp
  {
    public Constrained(Assembler aAsmblr) : base(aAsmblr)
    {
    }

    public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
    {
      var xOpType = aOpCode as OpType;
      DoExecute(Assembler, aMethod, aOpCode, xOpType, DebugEnabled);
    }

    private void DoExecute(Assembler assembler, _MethodInfo aMethod, ILOpCode aOpCode, OpType aTargetType, bool debugEnabled)
    {
      var xType = aTargetType.Value;

      XS.Comment($"Type = {aTargetType.Value}");
      if (xType.BaseType == Base.ValueType || xType.IsValueType || xType == BaseTypes.String)
      {
        return;
      }

      if (xType.BaseType == BaseTypes.Object)
      {
        throw new NotImplementedException($"Constrained not implemented for {aTargetType.Value}");
      }
    }
  }
}
