using System;
using Cosmos.IL2CPU.CIL.Utils;
using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.CIL.Instructions
{
    /// <summary>
    /// Convert to int64, pushing int64 on stack.
    /// </summary>
    [OpCode(ILOpCode.Code.Conv_I8)]
    public class Conv_I8 : ILOp
    {
        public Conv_I8(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xSource = aOpCode.StackPopTypes[0];
            var xSourceSize = SizeOfType(xSource);
            var xSourceIsFloat = TypeIsFloat(xSource);

            if (IsReferenceType(xSource))
            {
                // todo: Stop GC tracking
                XS.Add(ESP, SizeOfType(typeof(IntPtr)));

                // todo: x64
                XS.Pop(EAX);
                XS.Push(0);
                XS.Push(EAX);
            }
            else if (IsByRef(xSource))
            {
                // todo: Stop GC tracking
                throw new NotImplementedException($"Error compiling '{GetLabel(aMethod)}': conv.i8 not implemented for byref types!");
            }
            else if (xSourceSize <= 4)
            {
                if (xSourceIsFloat)
                {
                    /*
                     * Sadly for x86 there is no way using SSE to convert a float to an Int64... in x64 we could use ConvertPD2DQAndTruncate with
                     * x64 register as a destination... so this one of the few cases in which we need the legacy FPU!
                     */
                    XS.FPU.FloatLoad(ESP, destinationIsIndirect: true, size: RegisterSize.Int32);
                    XS.Sub(ESP, 4);
                    XS.FPU.IntStoreWithTruncate(ESP, isIndirect: true, size: RegisterSize.Long64);
                }
                else
                {
                    XS.Pop(EAX);
                    XS.SignExtendAX(RegisterSize.Int32);
                    XS.Push(EDX);
                    XS.Push(EAX);
                }
            }
            else if (xSourceSize <= 8)
            {
                if (xSourceIsFloat)
                {
                    /*
                     * Sadly for x86 there is no way using SSE to convert a double to an Int64... in x64 we could use ConvertPD2DQAndTruncate with
                     * x64 register as a destination... so only in this case we need the legacy FPU!
                     */
                    XS.FPU.FloatLoad(ESP, destinationIsIndirect: true, size: RegisterSize.Long64);
                    XS.FPU.IntStoreWithTruncate(ESP, isIndirect: true, size: RegisterSize.Long64);
                }
            }
            else
            {
                throw new NotImplementedException("Cosmos.IL2CPU.x86->IL->Conv_I8.cs->Error: StackSize > 8 not supported");
            }
        }
    }
}
