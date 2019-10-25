using System;

using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
  /// <summary>
  /// Convert to int32, pushing int32 on stack.
  /// </summary>
  [OpCode(ILOpCode.Code.Conv_I4)]
  public class Conv_I4 : ILOp
  {
    public Conv_I4(Assembler aAsmblr)
        : base(aAsmblr)
    {
    }

    public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
    {
      var xSource = aOpCode.StackPopTypes[0];
      DoExecute(SizeOfType(xSource), TypeIsFloat(xSource), TypeIsSigned(xSource), false, Assembler, aMethod, aOpCode);
    }

    public static void DoExecute(uint xSourceSize, bool aIsFloat, bool xSourceIsSigned, bool checkOverflow, Assembler assembler, _MethodInfo aMethod, ILOpCode aOpCode)
    {
      var xBaseLabel = GetLabel(aMethod, aOpCode) + ".";
      var xSuccessLabel = xBaseLabel + "Success";
      var xOverflowLabel = xBaseLabel + "Overflow";
      var xPositiveLabel = xBaseLabel + "Positive";
      var xNegativeLabel = xBaseLabel + "Negative";
      if (xSourceSize <= 4)
      {
        if (aIsFloat)
        {
          XS.SSE.MoveSS(XMM0, ESP, sourceIsIndirect: true);
          XS.SSE.ConvertSS2SIAndTruncate(EAX, XMM0);
          XS.Set(ESP, EAX, destinationIsIndirect: true);
        }
        else
        {
          if(checkOverflow) 
          {
            ConvOverflowChecks.CheckOverflowForSmall(4, xSourceIsSigned, true, assembler, aMethod, aOpCode, xSuccessLabel, xOverflowLabel);
          }
        }
      }
      else if (xSourceSize <= 8)
      {
        if (aIsFloat)
        {
          XS.SSE2.MoveSD(XMM0, ESP, sourceIsIndirect: true);
          XS.Add(ESP, 4);
          XS.SSE2.ConvertSD2SIAndTruncate(EAX, XMM0);
          XS.Set(ESP, EAX, destinationIsIndirect: true);
        }
        else
        {
          if (checkOverflow)
          {
            ConvOverflowChecks.CheckOverflowForLong(4, xSourceIsSigned, true, assembler, aMethod, aOpCode, xSuccessLabel, xOverflowLabel, xPositiveLabel, xNegativeLabel);
          }
          XS.Pop(EAX);
          XS.Add(ESP, 4);
          XS.Push(EAX);
        }
      }
      else
      {
        throw new NotImplementedException("Cosmos.IL2CPU.x86->IL->Conv_I4.cs->Error: StackSize > 8 not supported");
      }
    }
  }
}
