using System;

using IL2CPU.API;

using XSharp;
using XSharp.Assembler;
using XSharp.Assembler.x86;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Ckfinite)]
    public class Ckfinite : ILOp
    {
        public Ckfinite(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xStackType = aOpCode.StackPopTypes[0];

            var xNoThrowLabel = GetLabel(aMethod, aOpCode) + ".NoThrow";

            switch (SizeOfType(xStackType))
            {
                case 4:
                    XS.Pop(EAX);
                    XS.And(EAX, 0x7FFFFFFF);

                    XS.Compare(EAX, 0x7F800000);
                    XS.Jump(ConditionalTestEnum.Below, xNoThrowLabel);

                    XS.SSE2.ConvertSS2SD(XMM0, EAX);
                    XS.Sub(ESP, 8);
                    XS.SSE2.MoveSD(ESP, XMM0, true);

                    break;
                case 8:
                    XS.Set(EAX, ESP, sourceDisplacement: 4);

                    XS.And(EAX, 0x7FFFFFFF);
                    XS.Compare(EAX, 0x7F800000);
                    XS.Jump(ConditionalTestEnum.Below, xNoThrowLabel);

                    break;
                default:
                    throw new NotImplementedException();
            }
            
            XS.Call(LabelName.Get(ExceptionHelperRefs.ThrowNotFiniteNumberExceptionRef));

            XS.Label(xNoThrowLabel);
        }
    }
}
