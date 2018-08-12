using Cosmos.IL2CPU.ILOpCodes;
using IL2CPU.Reflection;

using XSharp;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
  [OpCode(ILOpCode.Code.Starg)]
  public class Starg : ILOp
  {
    public Starg(XSharp.Assembler.Assembler aAsmblr)
      : base(aAsmblr)
    {
    }

    public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
    {
      var xOpVar = (OpVar)aOpCode;
      DoExecute(Assembler, aMethod, xOpVar.Value);
    }

    public static void DoExecute(XSharp.Assembler.Assembler Assembler, _MethodInfo aMethod, ushort aParam)
    {
      var xDisplacement = Ldarg.GetArgumentDisplacement(aMethod, aParam);
      TypeInfo xArgType;
      if (aMethod.MethodInfo.IsStatic)
      {
        xArgType = aMethod.MethodInfo.ParameterTypes[aParam];
      }
      else
      {
        if (aParam == 0u)
        {
          xArgType = aMethod.MethodInfo.DeclaringType;
          if (xArgType.IsValueType)
          {
            xArgType = xArgType.MakeByReferenceType();
          }
        }
        else
        {
          xArgType = aMethod.MethodInfo.ParameterTypes[aParam - 1];
        }
      }

      XS.Comment("Arg idx = " + aParam);
      uint xArgRealSize = SizeOfType(xArgType);
      uint xArgSize = Align(xArgRealSize, 4);
      XS.Comment("Arg type = " + xArgType);
      XS.Comment("Arg real size = " + xArgRealSize + " aligned size = " + xArgSize);

      for (int i = (int)(xArgSize / 4) - 1; i >= 0; i--)
      {
        XS.Pop(EAX);
        XS.Set(EBP, EAX, destinationIsIndirect: true, destinationDisplacement: xDisplacement - (i * 4));
      }
    }
  }
}
