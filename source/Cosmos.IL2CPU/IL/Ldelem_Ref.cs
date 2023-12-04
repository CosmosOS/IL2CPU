using System;

using CPUx86 = XSharp.Assembler.x86;
using IL2CPU.API;
using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
  [OpCode(ILOpCode.Code.Ldelem_Ref)]
  public class Ldelem_Ref : ILOp
  {
    public Ldelem_Ref(Assembler aAsmblr)
        : base(aAsmblr)
    {
    }

    public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
    {
      Assemble(Assembler, 8, false, aMethod, aOpCode, DebugEnabled);
    }

    public static void Assemble(Assembler aAssembler, uint aElementSize, bool isSigned, Il2cpuMethodInfo aMethod, ILOpCode aOpCode, bool debugEnabled)
    {
      //  stack     = index
      //  stack + 2 = array
      DoNullReferenceCheck(aAssembler, debugEnabled, 8);

      var xBaseLabel = GetLabel(aMethod, aOpCode);
      var xNoIndexOutOfRangeExeptionLabel = xBaseLabel + "_NoIndexOutOfRangeException";
      var xIndexOutOfRangeExeptionLabel = xBaseLabel + "_IndexOutOfRangeException";
      XS.Pop(RBX); //get Position _, array, 0, index -> _, array, 0
      XS.Push(RSP, true, 4); // _, array, 0 => _, array, 0, array
      XS.Push(RSP, true, 12); // _, array, 0, array => _, array, 0, array, 0
      Ldlen.Assemble(aAssembler, debugEnabled, false); // _, array, 0, array, 0 -> _, array, 0, length
      XS.Pop(RAX); //Length of array _, array, 0, length -> _, array, 0
      XS.Compare(RAX, RBX);
      XS.Jump(CPUx86.ConditionalTestEnum.LessThanOrEqualTo, xIndexOutOfRangeExeptionLabel);

      XS.Compare(RBX, 0);
      XS.Jump(CPUx86.ConditionalTestEnum.GreaterThanOrEqualTo, xNoIndexOutOfRangeExeptionLabel);

      XS.Label(xIndexOutOfRangeExeptionLabel);
      XS.Pop(RAX);
      XS.Pop(RAX);
      Call.DoExecute(aAssembler, aMethod, ExceptionHelperRefs.ThrowIndexOutOfRangeException, aOpCode, xNoIndexOutOfRangeExeptionLabel, debugEnabled);

      XS.Label(xNoIndexOutOfRangeExeptionLabel);
      XS.Push(RBX); //_, array, 0 -> _, array, 0, index

      // calculate element offset into array memory (including header)
      XS.Pop(RAX);
      XS.Set(RDX, aElementSize);
      XS.Multiply(RDX);
      XS.Add(RAX, ObjectUtils.FieldDataOffset + 4);

      if (aElementSize > 4)
      {
        // we start copying the last bytes
        XS.Add(RAX, aElementSize - 4);
      }

      // pop the array now
      XS.Add(RSP, 4);
      XS.Pop(RDX);

      XS.Add(RDX, RAX);

      var xSizeLeft = aElementSize;
      while (xSizeLeft > 0)
      {
        var xCurrentStep = Math.Min(xSizeLeft, 4);
        if (xSizeLeft % 4 != 0)
        {
          xCurrentStep = xSizeLeft % 4;
        }

        xSizeLeft = xSizeLeft - xCurrentStep;
        switch (xCurrentStep)
        {
          case 1:
            if (isSigned)
            {
              XS.MoveSignExtend(RCX,RDX, sourceIsIndirect: true, size: RegisterSize.Byte8);
            }
            else
            {
              XS.MoveZeroExtend(RCX, RDX, sourceIsIndirect: true, size: RegisterSize.Byte8);
            }
            XS.Push(RCX);
            break;
          case 2:
            if (isSigned)
            {
              XS.MoveSignExtend(RCX, RDX, sourceIsIndirect: true, size: RegisterSize.Short16);
            }
            else
            {
              XS.MoveZeroExtend(RCX, RDX, sourceIsIndirect: true, size: RegisterSize.Short16);
            }
            XS.Push(RCX);
            break;
          case 4:
            // copy a full dword
            XS.Push(RDX, true);
            XS.Sub(RDX, 4); // move to previous 4 bytes
            break;
            //case 8:
            //    new CPUx86.Push {DestinationReg = CPUx86.Registers.EDX, DestinationDisplacement = 4, DestinationIsIndirect = true};
            //    XS.Push(XSRegisters.EDX, isIndirect: true);
            //    break;
        }
      }
    }
  }
}
