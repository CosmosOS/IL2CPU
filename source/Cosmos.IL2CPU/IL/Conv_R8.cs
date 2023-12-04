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

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xSource = aOpCode.StackPopTypes[0];
            var xSourceSize = SizeOfType(xSource);
            var xSourceIsFloat = TypeIsFloat(xSource);

            if (xSourceSize <= 4)
            {
                if (xSourceIsFloat)
                {
                    XS.SSE2.ConvertSS2SD(XMM0, RSP, sourceIsIndirect: true);
                    XS.Sub(RSP, 4);
                    XS.SSE2.MoveSD(RSP, XMM0, destinationIsIndirect: true);
                }
                else
                {
                    if (xSourceSize <= 2 || TypeIsSigned(xSource))
                    {
                        XS.SSE2.ConvertSI2SD(XMM0, RSP, sourceIsIndirect: true);
                        XS.Sub(RSP, 4);
                        XS.SSE2.MoveSD(RSP, XMM0, destinationIsIndirect: true);
                    }
                    else
                    {
                        throw new NotSupportedException($"OpCode data: xSource={xSource}, xSourceSize={xSourceSize}, xSourceIsFloat={xSourceIsFloat}");
                    }
                }
            }
            else if (xSourceSize <= 8)
            {
                if (!xSourceIsFloat)
                {
                    if (TypeIsSigned(xSource))
                    {
                        XS.FPU.IntLoad(RSP, isIndirect: true, size: RegisterSize.Long64);
                        XS.FPU.FloatStoreAndPop(RSP, isIndirect: true, size: RegisterSize.Long64);
                    }
                    else
                    {
                        throw new NotSupportedException($"OpCode data: xSource={xSource}, xSourceSize={xSourceSize}, xSourceIsFloat={xSourceIsFloat}");
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
