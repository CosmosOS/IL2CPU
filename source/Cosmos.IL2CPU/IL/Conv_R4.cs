using System;

using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    /// <summary>
    /// Convert to float32, pushing F on stack.
    /// </summary>
    [OpCode(ILOpCode.Code.Conv_R4)]
    public class Conv_R4 : ILOp
    {
        public Conv_R4(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xSource = aOpCode.StackPopTypes[0];
            var xSourceSize = SizeOfType(xSource);
            var xSourceIsFloat = TypeIsFloat(xSource);

            if (xSourceSize <= 4)
            {
                if (!xSourceIsFloat)
                {
                    if (xSourceSize <= 2 || TypeIsSigned(xSource))
                    {
                        XS.SSE.ConvertSI2SS(XMM0, ESP, sourceIsIndirect: true);
                        XS.SSE.MoveSS(ESP, XMM0, destinationIsIndirect: true);
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
            }
            else if (xSourceSize <= 8)
            {
                if (xSourceIsFloat)
                {
                    XS.SSE2.ConvertSD2SS(XMM0, ESP, sourceIsIndirect: true);
                    XS.Add(ESP, 4);
                    XS.SSE.MoveSS(ESP, XMM0, destinationIsIndirect: true);
                }
                else
                {
                    if (TypeIsSigned(xSource))
                    {
                        /*
                         * Again there is no SSE instruction in x86 to do this conversion as we need a 64 Bit register to do this! So we are forced
                         * to use the legacy x87 FPU to do this operation. In x64 the SSE instruction ConvertSIQ2SS should exist.
                         */
                        XS.FPU.IntLoad(ESP, isIndirect: true, size: RegisterSize.Long64);
                        XS.Add(ESP, 4);
                        /* This instruction is not needed FloatStoreAndPop does already the conversion */
                        // XS.SSE2.ConvertSD2SS(XMM0, ESP, sourceIsIndirect: true);
                        XS.FPU.FloatStoreAndPop(ESP, isIndirect: true, size: RegisterSize.Int32);
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
            }
            else
            {
                throw new NotImplementedException("Cosmos.IL2CPU.x86->IL->Conv_R4.cs->Error: StackSize > 8 not supported");
            }
        }
    }
}
