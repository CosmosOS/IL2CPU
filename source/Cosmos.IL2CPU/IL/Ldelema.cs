using System.Linq;
using CPUx86 = XSharp.Assembler.x86;

using Cosmos.IL2CPU.ILOpCodes;
using IL2CPU.API;
using XSharp;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
  [Cosmos.IL2CPU.OpCode(ILOpCode.Code.Ldelema)]
  public class Ldelema : ILOp
  {
    public Ldelema(XSharp.Assembler.Assembler aAsmblr)
      : base(aAsmblr)
    {
    }

    public static void Assemble(XSharp.Assembler.Assembler aAssembler, OpType aOpType, uint aElementSize, bool debugEnabled, Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
    {
      XS.Comment("Arraytype: " + aOpType.StackPopTypes.Last().FullName);
      XS.Comment("Size: " + aElementSize);

      DoNullReferenceCheck(aAssembler, debugEnabled, 8);

      //Do check for index out of range
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

      // Check if EBX is negative
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
      XS.Add(RAX, (uint)(ObjectUtils.FieldDataOffset + 4));

      // pop the array now
      XS.Add(RSP, 4);
      XS.Pop(RDX);

      XS.Add(RDX, RAX);
      XS.Push(RDX);
    }

    public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
    {
      var xOpType = (OpType)aOpCode;
      var xSize = SizeOfType(xOpType.Value);
      Assemble(Assembler, xOpType, xSize, DebugEnabled, aMethod, aOpCode);
    }
  }
}
