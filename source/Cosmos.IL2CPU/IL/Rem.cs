using System;

using XSharp;
using XSharp.Assembler;
using XSharp.Assembler.x86;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Rem)]
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
                    XS.SSE2.MoveSD(XMM0, RSP, sourceIsIndirect: true);
                    XS.Add(RSP, 8);
                    XS.SSE2.MoveSD(XMM1, RSP, sourceIsIndirect: true);
                    XS.SSE2.XorPD(XMM2, XMM2);
                    XS.SSE2.DivSD(XMM1, XMM0);
                    XS.SSE2.MoveSD(RSP, XMM2, destinationIsIndirect: true);
                }
                else
                {
                    string BaseLabel = GetLabel(aMethod, aOpCode) + ".";
                    string LabelShiftRight = BaseLabel + "ShiftRightLoop";
                    string LabelNoLoop = BaseLabel + "NoLoop";
                    string LabelEnd = BaseLabel + "End";

                    // divisor
                    //low
                    XS.Set(RSI, RSP, sourceIsIndirect: true);
                    //high
                    XS.Set(RDI, RSP, sourceDisplacement: 4);

                    XS.Xor(RAX, RAX);
                    XS.Or(RAX, RSI);
                    XS.Or(RAX, RDI);
                    XS.Jump(ConditionalTestEnum.NotZero, xNoDivideByZeroExceptionLabel);

                    XS.Call(GetLabel(ExceptionHelperRefs.ThrowDivideByZeroExceptionRef));

                    XS.Label(xNoDivideByZeroExceptionLabel);

                    //dividend
                    // low
                    XS.Set(RAX, RSP, sourceDisplacement: 8);
                    //high
                    XS.Set(RDX, RSP, sourceDisplacement: 12);

                    // pop both 8 byte values
                    XS.Add(RSP, 16);

                    // set flags
                    XS.Or(RDI, RDI);

                    // if high dword of divisor is already zero, we dont need the loop
                    XS.Jump(ConditionalTestEnum.Zero, LabelNoLoop);

                    // set ecx to zero for counting the shift operations
                    XS.Xor(RCX, RCX);

                    // push most significant bit of dividend, because the sign of the remainder is the sign of the dividend
                    XS.Set(RBX, RDX);
                    XS.Push(RBX);

                    XS.Compare(RDI, 0x80000000);
                    XS.Jump(ConditionalTestEnum.Below, BaseLabel + "divisor_no_neg");

                    XS.Negate(RSI);
                    XS.AddWithCarry(RDI, 0);
                    XS.Negate(RDI);

                    XS.Label(BaseLabel + "divisor_no_neg");

                    XS.Compare(RDX, 0x80000000);
                    XS.Jump(ConditionalTestEnum.Below, BaseLabel + "dividend_no_neg");

                    XS.Negate(RAX);
                    XS.AddWithCarry(RDX, 0);
                    XS.Negate(RDX);

                    XS.Label(BaseLabel + "dividend_no_neg");

                    XS.Label(LabelShiftRight);

                    // shift divisor 1 bit right
                    XS.ShiftRightDouble(RSI, RDI, 1);
                    XS.ShiftRight(RDI, 1);

                    // increment shift counter
                    XS.Increment(RCX);

                    // set flags
                    //XS.Or(EDI, EDI);
                    XS.Set(RBX, RSI);
                    XS.And(RBX, 0x80000000);
                    XS.Or(RBX, RDI);
                    // loop while high dword of divisor is not zero or most significant bit of low dword of divisor is set
                    XS.Jump(ConditionalTestEnum.NotZero, LabelShiftRight);

                    // shift the dividend now in one step
                    XS.ShiftRightDouble(RAX, RDX, CL);
                    // shift dividend CL bits right
                    XS.ShiftRight(RDX, CL);

                    // so we shifted both, so we have near the same relation as original values
                    // divide this
                    XS.Divide(RSI);

                    XS.Xor(RAX, RAX);

                    // shift the remainder in one step
                    XS.ShiftLeftDouble(RAX, RDX, CL);
                    // shift lower dword of remainder CL bits left
                    XS.ShiftLeft(RDX, CL);

                    // pop most significant bit of result
                    XS.Pop(RBX);

                    XS.Compare(RBX, 0x80000000);
                    XS.Jump(ConditionalTestEnum.Below, BaseLabel + "_remainder_no_neg");

                    XS.Negate(RDX);
                    XS.AddWithCarry(RAX, 0);
                    XS.Negate(RAX);

                    XS.Label(BaseLabel + "_remainder_no_neg");

                    // save result to stack
                    XS.Push(RAX);
                    XS.Push(RDX);

                    //TODO: implement proper derivation correction and overflow detection

                    XS.Jump(LabelEnd);

                    XS.Label(LabelNoLoop);
                    //save high dividend
                    XS.Set(RCX, RAX);
                    XS.Set(RAX, RDX);
                    // extend that sign is in edx
                    XS.SignExtendAX(RegisterSize.Long64);
                    // divide high part
                    XS.IntegerDivide(RSI);
                    XS.Set(RAX, RCX);
                    // divide low part
                    XS.Divide(RSI);
                    // save low result
                    XS.Push(0);
                    XS.Push(RDX);

                    XS.Label(LabelEnd);
                }
            }
            else
            {
                if (xIsFloat)
                {
                    XS.SSE.MoveSS(XMM0, RSP, sourceIsIndirect: true);
                    XS.Add(RSP, 4);
                    XS.SSE.MoveSS(XMM1, RSP, sourceIsIndirect: true);
                    XS.Add(RSP, 4);
                    XS.SSE.XorPS(XMM2, XMM2);
                    XS.SSE.DivSS(XMM1, XMM0);
                    XS.Sub(RSP, 4);
                    XS.SSE.MoveSS(RSP, XMM2, destinationIsIndirect: true);
                }
                else
                {
                    XS.Pop(RCX);

                    XS.Test(RCX, RCX);
                    XS.Jump(ConditionalTestEnum.NotZero, xNoDivideByZeroExceptionLabel);

                    XS.Call(GetLabel(ExceptionHelperRefs.ThrowDivideByZeroExceptionRef));

                    XS.Label(xNoDivideByZeroExceptionLabel);

                    XS.Pop(RAX);

                    XS.SignExtendAX(RegisterSize.Long64);

                    XS.IntegerDivide(RCX);
                    XS.Push(RDX);
                }
            }
        }
    }
}
