using IL2CPU.API;
using Cosmos.IL2CPU.ILOpCodes;
using IL2CPU.Reflection;

using XSharp;

namespace Cosmos.IL2CPU.X86.IL
{
  [Cosmos.IL2CPU.OpCode(ILOpCode.Code.Ldsflda)]
  public class Ldsflda : ILOp
  {
    public Ldsflda(XSharp.Assembler.Assembler aAsmblr)
      : base(aAsmblr)
    {
    }

    public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
    {
      var xOpCode = (OpField) aOpCode;
      var xFieldName = LabelName.GetStaticFieldName(xOpCode.Value);
      DoExecute(Assembler, aMethod, xFieldName, xOpCode.Value.DeclaringType, aOpCode);
    }

    public static void DoExecute(XSharp.Assembler.Assembler assembler, _MethodInfo aMethod, string field, TypeInfo declaringType, ILOpCode aCurrentOpCode)
    {
      // call cctor:
      var xCctor = (declaringType.GetTypeInitializer());
      if (xCctor != null)
      {
        XS.Call(LabelName.Get(xCctor));
        if (aCurrentOpCode != null)
        {
          ILOp.EmitExceptionLogic(assembler, aMethod, aCurrentOpCode, true, null, ".AfterCCTorExceptionCheck");
          XS.Label(".AfterCCTorExceptionCheck");
        }
      }
      string xDataName = field;
      XS.Push(xDataName);
    }
  }
}
