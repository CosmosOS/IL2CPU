using System;
using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
  /// <summary>
  /// Convert to unsigned int8, pushing int32 on stack.
  /// </summary>
  [OpCode(ILOpCode.Code.Conv_U1)]
  public class Conv_U1 : ILOp
  {
    public Conv_U1(Assembler aAsmblr)
        : base(aAsmblr)
    {
    }

    public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
    {
      var xSource = aOpCode.StackPopTypes[0];
      var xSourceIsFloat = TypeIsFloat(xSource);
      var xSourceSize = SizeOfType(xSource);

      DoExecute(xSourceIsFloat, xSourceSize, TypeIsSigned(xSource), false, Assembler, aMethod, aOpCode);
    }

    public static void DoExecute(bool xSourceIsFloat, uint xSourceSize, bool xSourceIsSigned, bool checkOverflow, Assembler assembler, Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
    {
      var xBaseLabel = GetLabel(aMethod, aOpCode) + ".";
      var xSuccessLabel = xBaseLabel + "Success";
      var xOverflowLabel = xBaseLabel + "Overflow";
      var xPositiveLabel = xBaseLabel + "Positive";
      var xNegativeLabel = xBaseLabel + "Negative";
      if (xSourceSize <= 4)
      {
        if (xSourceIsFloat)
        {
          XS.SSE.MoveSS(XMM0, ESP, sourceIsIndirect: true);
          XS.SSE.ConvertSS2SIAndTruncate(EAX, XMM0);
          XS.MoveZeroExtend(EAX, AL);
          XS.Set(ESP, EAX, destinationIsIndirect: true);
        }
        else
        {
          if (checkOverflow)
          {
            ConvOverflowChecks.CheckOverflowForSmall(1, xSourceIsSigned, false, assembler, aMethod, aOpCode, xSuccessLabel, xOverflowLabel);
          }
          XS.Pop(EAX);
          XS.MoveZeroExtend(EAX, AL);
          XS.Push(EAX);
        }
      }
      else if (xSourceSize <= 8)
      {
        if (xSourceIsFloat)
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
            ConvOverflowChecks.CheckOverflowForLong(1, xSourceIsSigned, false, assembler, aMethod, aOpCode, xSuccessLabel, xOverflowLabel, xPositiveLabel, xNegativeLabel);
          }
          XS.Pop(EAX);
          XS.Add(ESP, 4);
          XS.MoveZeroExtend(EAX, AL);
          XS.Push(EAX);
        }
      }
      else
      {
        throw new NotImplementedException("Cosmos.IL2CPU.x86->IL->Conv_U1.cs->Error: StackSize > 8 not supported");
      }
    }
  }
}
