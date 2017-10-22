using System;
using Cosmos.IL2CPU.ILOpCodes;
using CPUx86 = XSharp.Assembler.x86;
using CPU = XSharp.Assembler.x86;
using XSharp.Assembler;

using XSharp;

namespace Cosmos.IL2CPU.X86.IL
{
  [Cosmos.IL2CPU.OpCode(ILOpCode.Code.Ldtoken)]
  public class Ldtoken : ILOp
  {
    public Ldtoken(XSharp.Assembler.Assembler aAsmblr)
      : base(aAsmblr)
    {
    }

    public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
    {
      OpToken xToken = (OpToken) aOpCode;
      string xTokenAddress = null;

      if (xToken.ValueIsType)
      {
        xTokenAddress = ILOp.GetTypeIDLabel(xToken.ValueType);
      }
      if (xToken.ValueIsField)
      {
        xTokenAddress = DataMember.GetStaticFieldName(xToken.ValueField);
      }

      if (String.IsNullOrEmpty(xTokenAddress))
      {
        throw new Exception("Ldtoken not implemented!");
      }

      XS.Push(xTokenAddress);
      XS.Push(0);
    }
  }
}