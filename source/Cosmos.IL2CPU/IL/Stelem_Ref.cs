using System;
using Cosmos.Debug.Kernel;
using IL2CPU.API;
using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;
using CPUx86 = XSharp.Assembler.x86;

namespace Cosmos.IL2CPU.X86.IL
{
    [Cosmos.IL2CPU.OpCode(ILOpCode.Code.Stelem_Ref)]
    public class Stelem_Ref : ILOp
    {
        public Stelem_Ref(XSharp.Assembler.Assembler aAsmblr)
          : base(aAsmblr)
        {
        }

        public static void Assemble(Assembler aAssembler, uint aElementSize, Il2cpuMethodInfo aMethod, ILOpCode aOpCode, bool debugEnabled)
        {
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
            XS.Push(RSP, displacement: 4 + 4 + (int)xStackSize); // _, array, 0, index, value * n  => _, array, 0, index, value * n, array
            XS.Push(0); // _, array, 0, index, value * n, array => _, array, 0, index, value * n, array, 0
            Ldlen.Assemble(aAssembler, debugEnabled, false); // _, array, 0, index, value * n, array, 0 -> _, array, 0, index, value * n, length
            XS.Pop(RAX); //Length of array _, array, 0, index, value * n, length -> _, array, 0, index, value * n
            XS.Compare(RAX, RSP, sourceIsIndirect: true, sourceDisplacement: (int)xStackSize);
            XS.Jump(CPUx86.ConditionalTestEnum.LessThanOrEqualTo, xIndexOutOfRangeExeptionLabel);

            XS.Compare(RAX, 0);
            XS.Jump(CPUx86.ConditionalTestEnum.GreaterThanOrEqualTo, xNoIndexOutOfRangeExeptionLabel);

            XS.Label(xIndexOutOfRangeExeptionLabel);
            XS.Exchange(BX, BX);
            Call.DoExecute(aAssembler, aMethod, ExceptionHelperRefs.ThrowIndexOutOfRangeException, aOpCode, xNoIndexOutOfRangeExeptionLabel, debugEnabled);

            XS.Label(xNoIndexOutOfRangeExeptionLabel);

            // calculate element offset into array memory (including header)
            XS.Set(RAX, RSP, sourceDisplacement: (int)xStackSize); // the index
            XS.Set(RDX, aElementSize);
            XS.Multiply(RDX);
            XS.Add(RAX, ObjectUtils.FieldDataOffset + 4);

            XS.Set(RDX, RSP, sourceDisplacement: (int)xStackSize + 8); // the array
            XS.Add(RDX, RAX);
            XS.Push(RDX);

            XS.Pop(RCX);

            //get bytes
            var bytes = aElementSize / 4;
            for (uint i = bytes; i > 0; i -= 1)
            {
                new Comment(aAssembler, "Start 1 dword");
                XS.Pop(RBX);
                XS.Set(RCX, RBX, destinationIsIndirect: true);
                XS.Add(RCX, 4);
            }
            switch (aElementSize % 4)
            {
                case 1:
                    {
                        new Comment(aAssembler, "Start 1 byte");
                        XS.Pop(RBX);
                        XS.Set(RCX, BL, destinationIsIndirect: true);
                        break;
                    }
                case 2:
                    {
                        new Comment(aAssembler, "Start 1 word");
                        XS.Pop(RBX);
                        XS.Set(RCX, BX, destinationIsIndirect: true);
                        break;
                    }
                case 3:
                    {
                        new Comment(aAssembler, "Start 3 word");
                        XS.Pop(RBX);
                        XS.And(RBX, 0xFFFFFF); // Only take the value of the lower three bytes
                        XS.Set(RCX, RBX, destinationIsIndirect: true);
                        break;
                    }
                case 0:
                    {
                        break;
                    }
                default:
                    throw new Exception("Remainder size " + aElementSize % 4 + " not supported!");

            }

            XS.Add(RSP, 12);
        }
        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            Assemble(Assembler, 8, aMethod, aOpCode, DebugEnabled);
        }
    }
}
