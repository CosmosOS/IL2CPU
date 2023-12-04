using XSharp;
using XSharp.Assembler;
using XSharp.Assembler.x86;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
  class ConvOverflowChecks
  {
    public static void CheckOverflowForSmall(uint xResultSize, bool xSourceIsSigned, bool xResultIsSigned, Assembler assembler, Il2cpuMethodInfo aMethod, ILOpCode aOpCode, string xSuccessLabel, string xOverflowLabel)
    {
      XS.Set(RAX, RSP, sourceIsIndirect: true);
      // only look at bits which are part of result
      // normally check that they are all either 0 or 1
      // if same size but casting between signed and unsigned, then first bit must be zero
      byte bitCount = (byte)(xResultSize * 8 - 1);
      XS.ShiftRight(RAX, bitCount);
      XS.Compare(RAX, 0);
      XS.Jump(ConditionalTestEnum.Equal, xSuccessLabel);
      if (xSourceIsSigned)
      {
        if (xResultIsSigned)
        {
          XS.Not(RAX); // if negative then all must be 1s
          XS.Compare(RAX, 0);
          XS.Jump(ConditionalTestEnum.Equal, xSuccessLabel);
        }
        else
        {
          XS.Jump(xOverflowLabel);
        }
      }
      else // source was unsigned
      {
        if (xResultIsSigned)
        {
          XS.Jump(xOverflowLabel); //too big
        }
        else
        {
          XS.Compare(RAX, 1); // only lowest bit is set, which is heighest of next
          XS.Jump(ConditionalTestEnum.Equal, xSuccessLabel);
        }
      }
      XS.Label(xOverflowLabel);
      XS.Pop(RAX); // clear stack
      Call.DoExecute(assembler, aMethod, ExceptionHelperRefs.ThrowOverflowExceptionRef, aOpCode, xSuccessLabel, false);
      XS.Label(xSuccessLabel);
    }

    public static void CheckOverflowForLong(uint xResultSize, bool xSourceIsSigned, bool xResultIsSigned, Assembler assembler, Il2cpuMethodInfo aMethod, ILOpCode aOpCode, string xSuccessLabel, string xOverflowLabel, string xPositiveLabel, string xNegativeLabel)
    {
      // long is
      // low
      // high
      XS.Set(RAX, RSP, sourceIsIndirect: true, sourceDisplacement: 4); // read high
      if (xSourceIsSigned)
      {
        XS.ShiftRight(RAX, 31); // get highest bit of high to determine sign
        XS.Compare(RAX, 0);
        XS.Jump(ConditionalTestEnum.Equal, xPositiveLabel);
        XS.Compare(RAX, 1);
        XS.Jump(ConditionalTestEnum.Equal, xResultIsSigned ? xNegativeLabel : xOverflowLabel);
      }
      else
      {
        XS.Compare(RAX, 0); // as unsigned high must be positive
        XS.Jump(ConditionalTestEnum.Equal, xPositiveLabel);
      }
      XS.Label(xOverflowLabel);
      XS.Pop(RAX); //remove long from stack
      XS.Pop(RAX);
      Call.DoExecute(assembler, aMethod, ExceptionHelperRefs.ThrowOverflowExceptionRef, aOpCode, xSuccessLabel, false);

      // Positive check
      XS.Label(xPositiveLabel);
      XS.Set(RAX, RSP, sourceIsIndirect: true, sourceDisplacement: 4); // read high to refresh
      XS.Set(RBX, RSP, sourceIsIndirect: true); // read low
      XS.Compare(RAX, 0);
      XS.Jump(ConditionalTestEnum.NotEqual, xOverflowLabel);
      if(xResultSize == 4 && !xResultIsSigned)
      {
        XS.Jump(xSuccessLabel);
      }
      else
      {
        var v = xResultSize * 8;
        if (xResultIsSigned)
        {
          v -= 1;
        }
        XS.ShiftRight(RBX, (byte)v); // now check if low does not overflow
        XS.Compare(RBX, 0);
        XS.Jump(ConditionalTestEnum.NotEqual, xOverflowLabel);
        XS.Jump(xSuccessLabel);
      }

      //Negative check
      if (xSourceIsSigned)
      {
        XS.Label(xNegativeLabel);
        if(!xResultIsSigned)
        {
          XS.Jump(xOverflowLabel);
        }
        XS.Set(RAX, RSP, sourceIsIndirect: true, sourceDisplacement: 4); // read high to refresh
        XS.Compare(RAX, 0xffff_ffff); // high should be fully set
        XS.Jump(ConditionalTestEnum.NotEqual, xOverflowLabel);
        XS.Set(RBX, RSP, sourceIsIndirect: true); // read low
        XS.ShiftRight(RBX, (byte)(xResultSize * 8)); // now check if low does not overflow
        XS.Not(RBX);
        XS.Compare(RBX, 0);
        XS.Jump(ConditionalTestEnum.NotEqual, xOverflowLabel);
      }

      XS.Label(xSuccessLabel);
    }
  }

}
