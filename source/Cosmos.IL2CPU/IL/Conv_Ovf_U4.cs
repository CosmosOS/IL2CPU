using System;

using XSharp;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Conv_Ovf_U4)]
    public class Conv_Ovf_U4 : ILOp
    {
        public Conv_Ovf_U4(XSharp.Assembler.Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            var sourceType = aOpCode.StackPopTypes[0];

            var size = SizeOfType(sourceType);
            var isFloat = TypeIsFloat(sourceType);
            
            if (size <= 4)
            {
                if (isFloat)
                {
                    XS.SSE.MoveSS(XMM0, ESP, sourceIsIndirect: true);
                    XS.SSE.ConvertSS2SIAndTruncate(EAX, XMM0);
                    XS.Set(ESP, EAX, destinationIsIndirect: true);
                }
            }
            else if (size <= 8)
            {
                if (isFloat)
                {
                    XS.SSE2.MoveSD(XMM0, ESP, sourceIsIndirect: true);
                    XS.Add(ESP, 4);
                    XS.SSE2.ConvertSD2SIAndTruncate(EAX, XMM0);
                    XS.Set(ESP, EAX, destinationIsIndirect: true);
                }
                else
                {
                    XS.Add(ESP, 8);
                    XS.Push(ESP, displacement: -8);
                }
            }
            else
            {
                throw new NotImplementedException("Cosmos.IL2CPU.x86->IL->Conv_Ovf_U4.cs->Error: StackSize > 8 not supported");
            }
        }
    }
}
