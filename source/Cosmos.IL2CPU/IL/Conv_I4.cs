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

    public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
    {
      var xSource = aOpCode.StackPopTypes[0];
      DoExecute(SizeOfType(xSource), TypeIsFloat(xSource), TypeIsSigned(xSource), false, Assembler, aMethod, aOpCode);
    }

    public static void DoExecute(uint xSourceSize, bool aIsFloat, bool xSourceIsSigned, bool checkOverflow, Assembler assembler, Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
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
          XS.SSE.MoveSS(XMM0, RSP, sourceIsIndirect: true);
          XS.SSE.ConvertSS2SIAndTruncate(RAX, XMM0);
          XS.Set(RSP, RAX, destinationIsIndirect: true);
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
          XS.SSE2.MoveSD(XMM0, RSP, sourceIsIndirect: true);
          XS.Add(RSP, 4);
          XS.SSE2.ConvertSD2SIAndTruncate(RAX, XMM0);
          XS.Set(RSP, RAX, destinationIsIndirect: true);
        }
        else
        {
          if (checkOverflow)
          {
            ConvOverflowChecks.CheckOverflowForLong(4, xSourceIsSigned, true, assembler, aMethod, aOpCode, xSuccessLabel, xOverflowLabel, xPositiveLabel, xNegativeLabel);
          }
          XS.Pop(RAX);
          XS.Add(RSP, 4);
          XS.Push(RAX);
        }
      }
      else
      {
        throw new NotImplementedException("Cosmos.IL2CPU.x86->IL->Conv_I4.cs->Error: StackSize > 8 not supported");
      }
    }
  }
}
