using System;
using Cosmos.IL2CPU.CIL.Utils;
using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.CIL.Instructions
{
  public class Conv_Ovf_I8 : ILOp
  {
    public Conv_Ovf_I8(XSharp.Assembler.Assembler aAsmblr)
        : base(aAsmblr)
    {
    }

    public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
    {
      var xSource = aOpCode.StackPopTypes[0];
      var xSourceSize = SizeOfType(xSource);
      var xSourceIsFloat = TypeIsFloat(xSource);
      DoExecute(xSourceSize, false, Assembler, aMethod,aOpCode);
    }

    public static void DoExecute(uint xSourceSize, bool SourceIsSigned, Assembler assembler, Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
    {
      var xBaseLabel = GetLabel(aMethod, aOpCode) + ".";
      var xSuccessLabel = xBaseLabel + "Success";
      switch (xSourceSize)
      {
        case 1:
        case 2:
        case 4:
          XS.Pop(EAX);
          XS.SignExtendAX(RegisterSize.Int32);
          XS.Push(EDX);
          XS.Push(EAX);
          break;
        case 8:
          if (SourceIsSigned)
          {
            XS.Set(EAX, ESP, sourceIsIndirect: true);
            XS.And(EAX, 0b1000000000000000000000000000);
            XS.Compare(EAX, 0);
            XS.Jump(XSharp.Assembler.x86.ConditionalTestEnum.Equal, xSuccessLabel);
            XS.Pop(EAX); // remove long from stack
            XS.Pop(EAX);
            Call.DoExecute(assembler, aMethod, ExceptionHelperRefs.ThrowOverflowExceptionRef, aOpCode, xSuccessLabel, false);
            XS.Label(xSuccessLabel);
          }
          else
          {
            XS.Noop();
          }
          break;
        default:
          throw new NotImplementedException();
      }
    }
  }
}
