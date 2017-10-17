using Cosmos.IL2CPU.ILOpCodes;
using System;
using System.Reflection;
using XSharp;
using XSharp.Assembler;

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
      if (xType.GetTypeInfo().BaseType == typeof(ValueType) || xType.GetTypeInfo().IsValueType || xType == typeof(string))
      {
        return;
      }

      if (xType.GetTypeInfo().BaseType == typeof(object))
      {
        throw new NotImplementedException($"Constrained not implemented for {aTargetType.Value}");
      }
    }
  }
}
