using System;

using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    /// <summary>
    /// Convert to float64, pushing F on stack.
    /// </summary>
    [OpCode(ILOpCode.Code.Conv_R8)]
    public class Conv_R8 : ILOp
    {
        public Conv_R8(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            var xSource = aOpCode.StackPopTypes[0];
            var xSourceSize = SizeOfType(xSource);
            var xSourceIsFloat = TypeIsFloat(xSource);

            if (xSourceSize <= 4)
            {
                if (xSourceIsFloat)
                {
                    XS.SSE.ConvertSS2SD(XMM0, ESP, sourceIsIndirect: true);
                    XS.Sub(ESP, 4);
                    XS.SSE2.MoveSD(ESP, XMM0, destinationIsIndirect: true);
                }
                else
                {
                    if (xSourceSize <= 2 || IsIntegerSigned(xSource))
                    {
                        XS.SSE2.ConvertSI2SD(XMM0, ESP, sourceIsIndirect: true);
                        XS.Sub(ESP, 4);
                        XS.SSE2.MoveSD(ESP, XMM0, destinationIsIndirect: true);
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
            }
            else if (xSourceSize <= 8)
            {
                if (!xSourceIsFloat)
                {
                    if (IsIntegerSigned(xSource))
                    {
                        XS.FPU.IntLoad(ESP, isIndirect: true, size: RegisterSize.Long64);
                        XS.FPU.FloatStoreAndPop(ESP, isIndirect: true, size: RegisterSize.Long64);
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
            }
            else
            {
                throw new NotImplementedException("Cosmos.IL2CPU.x86->IL->Conv_R8.cs->Error: StackSize > 8 not supported");
            }
        }
    }
}
