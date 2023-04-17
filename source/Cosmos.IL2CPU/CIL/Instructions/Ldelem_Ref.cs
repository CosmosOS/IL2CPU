using System;
using Cosmos.IL2CPU.CIL.Utils;
using IL2CPU.API;
using XSharp;
using XSharp.Assembler;
using CPUx86 = XSharp.Assembler.x86;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.CIL.Instructions
{
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
      XS.Pop(EBX); //get Position _, array, 0, index -> _, array, 0
      XS.Push(ESP, true, 4); // _, array, 0 => _, array, 0, array
      XS.Push(ESP, true, 12); // _, array, 0, array => _, array, 0, array, 0
      Ldlen.Assemble(aAssembler, debugEnabled, false); // _, array, 0, array, 0 -> _, array, 0, length
      XS.Pop(EAX); //Length of array _, array, 0, length -> _, array, 0
      XS.Compare(EAX, EBX);
      XS.Jump(CPUx86.ConditionalTestEnum.LessThanOrEqualTo, xIndexOutOfRangeExeptionLabel);

      XS.Compare(EBX, 0);
      XS.Jump(CPUx86.ConditionalTestEnum.GreaterThanOrEqualTo, xNoIndexOutOfRangeExeptionLabel);

      XS.Label(xIndexOutOfRangeExeptionLabel);
      XS.Pop(EAX);
      XS.Pop(EAX);
      Call.DoExecute(aAssembler, aMethod, ExceptionHelperRefs.ThrowIndexOutOfRangeException, aOpCode, xNoIndexOutOfRangeExeptionLabel, debugEnabled);

      XS.Label(xNoIndexOutOfRangeExeptionLabel);
      XS.Push(EBX); //_, array, 0 -> _, array, 0, index

      // calculate element offset into array memory (including header)
      XS.Pop(EAX);
      XS.Set(EDX, aElementSize);
      XS.Multiply(EDX);
      XS.Add(EAX, ObjectUtils.FieldDataOffset + 4);

      if (aElementSize > 4)
      {
        // we start copying the last bytes
        XS.Add(EAX, aElementSize - 4);
      }

      // pop the array now
      XS.Add(ESP, 4);
      XS.Pop(EDX);

      XS.Add(EDX, EAX);

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
              XS.MoveSignExtend(ECX,EDX, sourceIsIndirect: true, size: RegisterSize.Byte8);
            }
            else
            {
              XS.MoveZeroExtend(ECX, EDX, sourceIsIndirect: true, size: RegisterSize.Byte8);
            }
            XS.Push(ECX);
            break;
          case 2:
            if (isSigned)
            {
              XS.MoveSignExtend(ECX, EDX, sourceIsIndirect: true, size: RegisterSize.Short16);
            }
            else
            {
              XS.MoveZeroExtend(ECX, EDX, sourceIsIndirect: true, size: RegisterSize.Short16);
            }
            XS.Push(ECX);
            break;
          case 4:
            // copy a full dword
            XS.Push(EDX, true);
            XS.Sub(EDX, 4); // move to previous 4 bytes
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
