using System;

using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    /// <summary>
    /// Convert to unsigned int8, pushing int32 on stack.
    /// </summary>
    [OpCode(ILOpCode.Code.Conv_U1)]
    public class Conv_U1 : ILOp
    {
        public Conv_U1(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            var xSource = aOpCode.StackPopTypes[0];
            var xSourceIsFloat = TypeIsFloat(xSource);
            var xSourceSize = SizeOfType(xSource);

            if (xSourceSize <= 4)
            {
                if (xSourceIsFloat)
                {
                    XS.SSE.MoveSS(XMM0, ESP, sourceIsIndirect: true);
                    XS.SSE.ConvertSS2SIAndTruncate(EAX, XMM0);
                    XS.MoveZeroExtend(EAX, AL);
                    XS.Set(ESP, EAX, destinationIsIndirect: true);
                }
                else
                {
                    XS.Pop(EAX);
                    XS.MoveZeroExtend(EAX, AL);
                    XS.Push(EAX);
                }
            }
            else if (xSourceSize <= 8)
            {
                if (xSourceIsFloat)
                {
                    XS.SSE2.MoveSD(XMM0, ESP, sourceIsIndirect: true);
                    XS.Add(ESP, 4);
                    XS.SSE2.ConvertSD2SIAndTruncate(EAX, XMM0);
                    XS.Set(ESP, EAX, destinationIsIndirect: true);
                }
                else
                {
                    XS.Pop(EAX);
                    XS.Add(ESP, 4);
                    XS.MoveZeroExtend(EAX, AL);
                    XS.Push(EAX);
                }
            }
            else
            {
                throw new NotImplementedException("Cosmos.IL2CPU.x86->IL->Conv_U1.cs->Error: StackSize > 8 not supported");
            }
        }
    }
}
