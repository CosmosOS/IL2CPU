using System;
using System.Linq;
using System.Reflection;
using Cosmos.IL2CPU.CIL.ILOpCodes;
using Cosmos.IL2CPU.CIL.Utils;
using IL2CPU.API;
using XSharp;

namespace Cosmos.IL2CPU.CIL.Instructions
{
  public class Ldsflda : ILOp
  {
    public Ldsflda(XSharp.Assembler.Assembler aAsmblr)
      : base(aAsmblr)
    {
    }

    public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
    {
      var xOpCode = (OpField) aOpCode;
      var xFieldName = LabelName.GetStaticFieldName(xOpCode.Value);
      DoExecute(Assembler, aMethod, xFieldName, xOpCode.Value.DeclaringType, aOpCode);
    }

    public static void DoExecute(XSharp.Assembler.Assembler assembler, Il2cpuMethodInfo aMethod, string field, Type declaringType, ILOpCode aCurrentOpCode)
    {
      // call cctor:
      var xCctor = (declaringType.GetConstructors(BindingFlags.Static | BindingFlags.NonPublic) ?? Array.Empty<ConstructorInfo>()).SingleOrDefault();
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
