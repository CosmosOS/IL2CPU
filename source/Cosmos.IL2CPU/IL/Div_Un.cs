using System;

using XSharp;
using XSharp.Assembler;
using XSharp.Assembler.x86;
using static XSharp.XSRegisters;

/* Div.Un is unsigned integer division so the valid input values are uint / ulong and the result is always expressed as unsigned */
namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Div_Un)]
    public class Div_Un : ILOp
    {
        public Div_Un(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xStackItem = aOpCode.StackPopTypes[0];
            var xSize = Math.Max(SizeOfType(xStackItem), SizeOfType(aOpCode.StackPopTypes[1]));
            var xBaseLabel = GetLabel(aMethod, aOpCode);
            var xNoDivideByZeroExceptionLabel = xBaseLabel + "_NoDivideByZeroException";

            if (TypeIsFloat(xStackItem))
            {
                throw new Exception("Cosmos.IL2CPU.x86->IL->Div_Un.cs->Error: Expected unsigned integer operands but got float!");
            }

            if (xSize > 8)
            {
                throw new NotImplementedException("Cosmos.IL2CPU.x86->IL->Div_Un.cs->Error: StackSize > 8 not supported");
            }
            else if (xSize > 4)
            {
                string BaseLabel = GetLabel(aMethod, aOpCode) + ".";
                string LabelShiftRight = BaseLabel + "ShiftRightLoop";
                string LabelNoLoop = BaseLabel + "NoLoop";
                string LabelEnd = BaseLabel + "End";

                // divisor
                // low
                XS.Pop(ESI);
                // high
                XS.Pop(EDI);

                XS.Xor(EAX, EAX);
                XS.Or(EAX, ESI);
                XS.Or(EAX, EDI);
                XS.Jump(ConditionalTestEnum.NotZero, xNoDivideByZeroExceptionLabel);

                XS.Call(GetLabel(ExceptionHelperRefs.ThrowDivideByZeroExceptionRef));

                XS.Label(xNoDivideByZeroExceptionLabel);

                // dividend
                // low
                XS.Pop(EAX);
                // high
                XS.Pop(EDX);

                // set flags
                XS.Or(EDI, EDI);
                // if high dword of divisor is already zero, we dont need the loop
                XS.Jump(ConditionalTestEnum.Zero, LabelNoLoop);

                // set ecx to zero for counting the shift operations
                XS.Xor(ECX, ECX);

                XS.Label(LabelShiftRight);

                // shift divisor 1 bit right
                XS.ShiftRightDouble(ESI, EDI, 1);
                XS.ShiftRight(EDI, 1);

                // increment shift counter
                XS.Increment(ECX);

                // set flags
                XS.Or(EDI, EDI);
                // loop while high dword of divisor till it is zero
                XS.Jump(ConditionalTestEnum.NotZero, LabelShiftRight);

                // shift the dividend now in one step
                // shift dividend CL bits right
                XS.ShiftRightDouble(EAX, EDX, CL);
                XS.ShiftRight(EDX, CL);

                // so we shifted both, so we have near the same relation as original values
                // divide this
                XS.Divide(ESI);

                // save result to stack
                XS.Push(0);
                XS.Push(EAX);

                //TODO: implement proper derivation correction and overflow detection

                XS.Jump(LabelEnd);

                XS.Label(LabelNoLoop);

                //save high dividend
                XS.Set(ECX, EAX);
                XS.Set(EAX, EDX);
                // zero EDX, so that high part is zero -> reduce overflow case
                XS.Xor(EDX, EDX);
                // divide high part
                XS.Divide(ESI);
                // save high result
                XS.Push(EAX);
                XS.Set(EAX, ECX);
                // divide low part
                XS.Divide(ESI);
                // save low result
                XS.Push(EAX);

                XS.Label(LabelEnd);
            }
            else
            {
                XS.Pop(ECX);

                XS.Test(ECX, ECX);
                XS.Jump(ConditionalTestEnum.NotZero, xNoDivideByZeroExceptionLabel);

                XS.Call(GetLabel(ExceptionHelperRefs.ThrowDivideByZeroExceptionRef));

                XS.Label(xNoDivideByZeroExceptionLabel);

                XS.Pop(EAX);

                XS.Xor(EDX, EDX);

                XS.Divide(ECX);
                XS.Push(EAX);
            }
        }
    }
}
