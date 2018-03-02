using System;
using XSharp.Assembler;

using XSharp;
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

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {

            var xStackItem = aOpCode.StackPopTypes[0];
            var xStackItemSize = SizeOfType(xStackItem);
            var xSize = Math.Max(xStackItemSize, SizeOfType(aOpCode.StackPopTypes[1]));

            if (xSize > 4)
            {
                if (TypeIsFloat(xStackItem))
                {
                    XS.SSE.MoveSS(XMM0, ESP, sourceIsIndirect: true);
                    XS.Add(ESP, 8);
                    XS.SSE.MoveSS(XMM1, ESP, sourceIsIndirect: true);
                    XS.SSE.XorPS(XMM2, XMM2);
                    XS.SSE.DivPS(XMM1, XMM0);
                    XS.SSE.MoveSS(ESP, XMM2, destinationIsIndirect: true);
                }
                else
                {
                    XS.FPU.IntLoad(ESP, displacement: 8, size: RegisterSize.Long64);
                    XS.FPU.IntLoad(ESP, isIndirect: true, size: RegisterSize.Long64);
                    XS.Sub(ESP, 8);

                    XS.LiteralCode("fdivp st1, st0");

                    XS.FPU.IntStoreWithTruncate(ESP, true, RegisterSize.Long64);

                    XS.FPU.IntLoad(ESP, isIndirect: true, size: RegisterSize.Long64);
                    XS.Add(ESP, 8);
                    XS.FPU.IntLoad(ESP, isIndirect: true, size: RegisterSize.Long64);

                    XS.LiteralCode("fmulp st1, st0");

                    XS.FPU.IntStoreWithTruncate(ESP, true, RegisterSize.Long64);
                    XS.FPU.IntLoad(ESP, displacement: 8, size: RegisterSize.Long64);
                    XS.FPU.IntLoad(ESP, isIndirect: true, size: RegisterSize.Long64);
                    XS.Add(ESP, 8);

                    XS.LiteralCode("fsubp st1, st0");

                    XS.FPU.IntStoreWithTruncate(ESP, true, RegisterSize.Long64);
                }
            }
            else
            {
                if (TypeIsFloat(xStackItem))
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
                    XS.Pop(EAX); // gets devised by ecx
                    XS.Xor(EDX, EDX);

                    XS.Divide(ECX); // => EAX / ECX
                    XS.Push(EDX);
                }
            }
        }
    }
}
