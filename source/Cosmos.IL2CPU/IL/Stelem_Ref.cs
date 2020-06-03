using System;
using CPUx86 = XSharp.Assembler.x86;
using XSharp.Assembler;
using static XSharp.XSRegisters;


using IL2CPU.API;
using XSharp;

namespace Cosmos.IL2CPU.X86.IL
{
  [Cosmos.IL2CPU.OpCode(ILOpCode.Code.Stelem_Ref)]
  public class Stelem_Ref : ILOp
  {
    public Stelem_Ref(XSharp.Assembler.Assembler aAsmblr)
      : base(aAsmblr)
    {
    }

    public static void Assemble(Assembler aAssembler, uint aElementSize, _MethodInfo aMethod, ILOpCode aOpCode, bool debugEnabled)
    {
      // stack     == the new value
      // stack + 1 == the index
      // stack + 2 == the array
      DoNullReferenceCheck(aAssembler, debugEnabled, (int)(8 + Align(aElementSize, 4)));

      uint xStackSize = aElementSize;
      if (xStackSize % 4 != 0)
      {
        xStackSize += 4 - xStackSize % 4;
      }
      // Do index out of range check
      var xBaseLabel = GetLabel(aMethod, aOpCode);
      var xNoIndexOutOfRangeExeptionLabel = xBaseLabel + "_NoIndexOutOfRangeException";
      var xIndexOutOfRangeExeptionLabel = xBaseLabel + "_IndexOutOfRangeException";
      if (xStackSize == 4)
      {
        XS.Pop(ECX); //get value _, array, 0, index, value -> _, array, 0, index
      }
      else if(xStackSize == 8)
      {
        XS.Pop(ECX); //get value _, array, 0, index, value0, value1 -> _, array, 0, index, value0
        XS.Pop(EDX); //get value _, array, 0, index, value0 -> _, array, 0, index
      }
      else
      {
        throw new NotImplementedException();
      }
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
      if (xStackSize == 4)
      {
        XS.Push(ECX); //_, array, 0 -> _, array, 0, index, value
      } else if(xStackSize == 8)
      {
        XS.Push(EDX); //_, array, 0, index -> _, array, 0, index, value0
        XS.Push(ECX); //_, array, 0, index, value0 -> _, array, 0, index, value0, value1
      }

      // calculate element offset into array memory (including header)
      XS.Set(EAX, ESP, sourceDisplacement: (int)xStackSize); // the index
      XS.Set(EDX, aElementSize);
      XS.Multiply(EDX);
      XS.Add(EAX, ObjectUtils.FieldDataOffset + 4);

      XS.Set(EDX, ESP, sourceDisplacement: (int)xStackSize + 8); // the array
      XS.Add(EDX, EAX);
      XS.Push(EDX);

      XS.Pop(ECX);
      for (int i = (int)(aElementSize / 4) - 1; i >= 0; i -= 1)
      {
        new Comment(aAssembler, "Start 1 dword");
        XS.Pop(EBX);
        XS.Set(ECX, EBX, destinationIsIndirect: true);
        XS.Add(ECX, 4);
      }
      switch (aElementSize % 4)
      {
        case 1:
          {
            new Comment(aAssembler, "Start 1 byte");
            XS.Pop(EBX);
            XS.Set(ECX, BL, destinationIsIndirect: true);
            break;
          }
        case 2:
          {
            new Comment(aAssembler, "Start 1 word");
            XS.Pop(EBX);
            XS.Set(ECX, BX, destinationIsIndirect: true);
            break;
          }
        case 0:
          {
            break;
          }
        default:
          throw new Exception("Remainder size " + (aElementSize % 4) + " not supported!");

      }
      XS.Add(ESP, 12);
    }
    public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
    {
      Assemble(Assembler, 8, aMethod, aOpCode, DebugEnabled);
    }
  }
}
