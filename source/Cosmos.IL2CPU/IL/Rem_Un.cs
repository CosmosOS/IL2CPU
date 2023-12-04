using System;

using XSharp;
using XSharp.Assembler;
using XSharp.Assembler.x86;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    /// <summary>
    /// Divides two unsigned values and pushes the remainder onto the evaluation stack.
    /// </summary>
    [OpCode(ILOpCode.Code.Rem_Un)]
    public class Rem_Un : ILOp
    {
        public Rem_Un(Assembler aAsmblr)
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
                throw new Exception("Cosmos.IL2CPU.x86->IL->Rem_Un.cs->Error: Expected unsigned integer operands but got float!");
            }

            if (xSize > 8)
            {
                throw new NotImplementedException("Cosmos.IL2CPU.x86->IL->Rem_Un.cs->Error: StackSize > 8 not supported");
            }
            else if (xSize > 4)
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

                XS.Label(LabelShiftRight);

                // shift divisor 1 bit right
                XS.ShiftRightDouble(RSI, RDI, 1);
                XS.ShiftRight(RDI, 1);

                // increment shift counter
                XS.Increment(RCX);

                // set flags
                XS.Or(RDI, RDI);
                // loop while high dword of divisor till it is zero
                XS.Jump(ConditionalTestEnum.NotZero, LabelShiftRight);

                // shift the dividend now in one step
                XS.ShiftRightDouble(RAX, RDX, CL);
                // shift dividend CL bits right
                XS.ShiftRight(RDX, CL);

                // so we shifted both, so we have near the same relation as original values
                // divide this
                XS.Divide(RSI);

                // set eax to zero
                XS.Xor(RAX, RAX);

                // shift the remainder in one step
                XS.ShiftLeftDouble(RAX, RDX, CL);
                // shift lower dword of remainder CL bits left
                XS.ShiftLeft(RDX, CL);

                // save remainder to stack
                XS.Push(RAX);
                XS.Push(RDX);

                //TODO: implement proper derivation correction and overflow detection

                XS.Jump(LabelEnd);

                XS.Label(LabelNoLoop);

                //save high dividend
                XS.Set(RCX, RAX);
                XS.Set(RAX, RDX);

                // zero EDX, so that high part is zero -> reduce overflow case
                XS.Xor(RDX, RDX);

                // divide high part
                XS.Divide(RSI);
                XS.Set(RAX, RCX);

                // divide low part
                XS.Divide(RSI);

                // save remainder result
                XS.Push(0);
                XS.Push(RDX);

                XS.Label(LabelEnd);
            }
            else
            {
                XS.Pop(RCX);

                XS.Test(RCX, RCX);
                XS.Jump(ConditionalTestEnum.NotZero, xNoDivideByZeroExceptionLabel);

                XS.Call(GetLabel(ExceptionHelperRefs.ThrowDivideByZeroExceptionRef));

                XS.Label(xNoDivideByZeroExceptionLabel);

                XS.Pop(RAX);

                XS.Xor(RDX, RDX);

                XS.Divide(RCX);
                XS.Push(RDX);
            }
        }
    }
}
