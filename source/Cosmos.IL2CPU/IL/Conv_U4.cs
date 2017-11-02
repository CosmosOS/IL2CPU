using System;

using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    /// <summary>
    /// Convert to unsigned int32, pushing int32 on stack.
    /// </summary>
    [OpCode(ILOpCode.Code.Conv_U4)]
    public class Conv_U4 : ILOp
    {
        public Conv_U4(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            var xSource = aOpCode.StackPopTypes[0];
            DoExecute(SizeOfType(xSource), TypeIsFloat(xSource));
        }

        public static void DoExecute(uint aSize, bool aIsFloat)
        {
            if (aSize <= 4)
            {
                if (aIsFloat)
                {
                    XS.SSE.MoveSS(XMM0, ESP, sourceIsIndirect: true);
                    XS.SSE.ConvertSS2SIAndTruncate(EAX, XMM0);
                    XS.Set(ESP, EAX, destinationIsIndirect: true);
                }
            }
            else if (aSize <= 8)
            {
                if (aIsFloat)
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
                    XS.Push(EAX);
                }
            }
            else
            {
                throw new NotImplementedException("Cosmos.IL2CPU.x86->IL->Conv_U4.cs->Error: StackSize > 8 not supported");
            }
        }
    }
}
