using System;
using Cosmos.IL2CPU.CIL.Utils;
using XSharp;
using XSharp.Assembler;
using XSharp.Assembler.x86;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.CIL.Instructions
{
    public class Rem : ILOp
    {
        public Rem(Assembler aAsmblr)
          : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xStackItem = aOpCode.StackPopTypes[0];
            var xSize = Math.Max(SizeOfType(xStackItem), SizeOfType(aOpCode.StackPopTypes[1]));
            var xIsFloat = TypeIsFloat(xStackItem);
            var xBaseLabel = GetLabel(aMethod, aOpCode);
            var xNoDivideByZeroExceptionLabel = xBaseLabel + "_NoDivideByZeroException";

            if (xSize > 8)
            {
                throw new NotImplementedException("Cosmos.IL2CPU.x86->IL->Rem.cs->Error: StackSize > 8 not supported");
            }
            else if (xSize > 4)
            {
                if (xIsFloat)
                {
                    XS.SSE2.MoveSD(XMM0, ESP, sourceIsIndirect: true);
                    XS.Add(ESP, 8);
                    XS.SSE2.MoveSD(XMM1, ESP, sourceIsIndirect: true);
                    XS.SSE2.XorPD(XMM2, XMM2);
                    XS.SSE2.DivSD(XMM1, XMM0);
                    XS.SSE2.MoveSD(ESP, XMM2, destinationIsIndirect: true);
                }
                else
                {
                    string BaseLabel = GetLabel(aMethod, aOpCode) + ".";
                    string LabelShiftRight = BaseLabel + "ShiftRightLoop";
                    string LabelNoLoop = BaseLabel + "NoLoop";
                    string LabelEnd = BaseLabel + "End";

                    // divisor
                    //low
                    XS.Set(ESI, ESP, sourceIsIndirect: true);
                    //high
                    XS.Set(EDI, ESP, sourceDisplacement: 4);

                    XS.Xor(EAX, EAX);
                    XS.Or(EAX, ESI);
                    XS.Or(EAX, EDI);
                    XS.Jump(ConditionalTestEnum.NotZero, xNoDivideByZeroExceptionLabel);

                    XS.Call(GetLabel(ExceptionHelperRefs.ThrowDivideByZeroExceptionRef));

                    XS.Label(xNoDivideByZeroExceptionLabel);

                    //dividend
                    // low
                    XS.Set(EAX, ESP, sourceDisplacement: 8);
                    //high
                    XS.Set(EDX, ESP, sourceDisplacement: 12);

                    // pop both 8 byte values
                    XS.Add(ESP, 16);

                    // set flags
                    XS.Or(EDI, EDI);

                    // if high dword of divisor is already zero, we dont need the loop
                    XS.Jump(ConditionalTestEnum.Zero, LabelNoLoop);

                    // set ecx to zero for counting the shift operations
                    XS.Xor(ECX, ECX);

                    // push most significant bit of dividend, because the sign of the remainder is the sign of the dividend
                    XS.Set(EBX, EDX);
                    XS.Push(EBX);

                    XS.Compare(EDI, 0x80000000);
                    XS.Jump(ConditionalTestEnum.Below, BaseLabel + "divisor_no_neg");

                    XS.Negate(ESI);
                    XS.AddWithCarry(EDI, 0);
                    XS.Negate(EDI);

                    XS.Label(BaseLabel + "divisor_no_neg");

                    XS.Compare(EDX, 0x80000000);
                    XS.Jump(ConditionalTestEnum.Below, BaseLabel + "dividend_no_neg");

                    XS.Negate(EAX);
                    XS.AddWithCarry(EDX, 0);
                    XS.Negate(EDX);

                    XS.Label(BaseLabel + "dividend_no_neg");

                    XS.Label(LabelShiftRight);

                    // shift divisor 1 bit right
                    XS.ShiftRightDouble(ESI, EDI, 1);
                    XS.ShiftRight(EDI, 1);

                    // increment shift counter
                    XS.Increment(ECX);

                    // set flags
                    //XS.Or(EDI, EDI);
                    XS.Set(EBX, ESI);
                    XS.And(EBX, 0x80000000);
                    XS.Or(EBX, EDI);
                    // loop while high dword of divisor is not zero or most significant bit of low dword of divisor is set
                    XS.Jump(ConditionalTestEnum.NotZero, LabelShiftRight);

                    // shift the dividend now in one step
                    XS.ShiftRightDouble(EAX, EDX, CL);
                    // shift dividend CL bits right
                    XS.ShiftRight(EDX, CL);

                    // so we shifted both, so we have near the same relation as original values
                    // divide this
                    XS.Divide(ESI);

                    XS.Xor(EAX, EAX);

                    // shift the remainder in one step
                    XS.ShiftLeftDouble(EAX, EDX, CL);
                    // shift lower dword of remainder CL bits left
                    XS.ShiftLeft(EDX, CL);

                    // pop most significant bit of result
                    XS.Pop(EBX);

                    XS.Compare(EBX, 0x80000000);
                    XS.Jump(ConditionalTestEnum.Below, BaseLabel + "_remainder_no_neg");

                    XS.Negate(EDX);
                    XS.AddWithCarry(EAX, 0);
                    XS.Negate(EAX);

                    XS.Label(BaseLabel + "_remainder_no_neg");

                    // save result to stack
                    XS.Push(EAX);
                    XS.Push(EDX);

                    //TODO: implement proper derivation correction and overflow detection

                    XS.Jump(LabelEnd);

                    XS.Label(LabelNoLoop);
                    //save high dividend
                    XS.Set(ECX, EAX);
                    XS.Set(EAX, EDX);
                    // extend that sign is in edx
                    XS.SignExtendAX(RegisterSize.Int32);
                    // divide high part
                    XS.IntegerDivide(ESI);
                    XS.Set(EAX, ECX);
                    // divide low part
                    XS.Divide(ESI);
                    // save low result
                    XS.Push(0);
                    XS.Push(EDX);

                    XS.Label(LabelEnd);
                }
            }
            else
            {
                if (xIsFloat)
                {
                    XS.SSE.MoveSS(XMM0, ESP, sourceIsIndirect: true);
                    XS.Add(ESP, 4);
                    XS.SSE.MoveSS(XMM1, ESP, sourceIsIndirect: true);
                    XS.Add(ESP, 4);
                    XS.SSE.XorPS(XMM2, XMM2);
                    XS.SSE.DivSS(XMM1, XMM0);
                    XS.Sub(ESP, 4);
                    XS.SSE.MoveSS(ESP, XMM2, destinationIsIndirect: true);
                }
                else
                {
                    XS.Pop(ECX);

                    XS.Test(ECX, ECX);
                    XS.Jump(ConditionalTestEnum.NotZero, xNoDivideByZeroExceptionLabel);

                    XS.Call(GetLabel(ExceptionHelperRefs.ThrowDivideByZeroExceptionRef));

                    XS.Label(xNoDivideByZeroExceptionLabel);

                    XS.Pop(EAX);

                    XS.SignExtendAX(RegisterSize.Int32);

                    XS.IntegerDivide(ECX);
                    XS.Push(EDX);
                }
            }
        }
    }
}
