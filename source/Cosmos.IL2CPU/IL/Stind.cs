using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Stind_I)]
    [OpCode(ILOpCode.Code.Stind_I1)]
    [OpCode(ILOpCode.Code.Stind_I2)]
    [OpCode(ILOpCode.Code.Stind_I4)]
    [OpCode(ILOpCode.Code.Stind_I8)]
    [OpCode(ILOpCode.Code.Stind_R4)]
    [OpCode(ILOpCode.Code.Stind_R8)]
    [OpCode(ILOpCode.Code.Stind_Ref)]
    public class Stind : ILOp
    {
        public Stind(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            switch (aOpCode.OpCode)
            {
                case ILOpCode.Code.Stind_I1:
                    XS.Pop(ECX);
                    XS.Pop(EAX);
                    XS.Set(EAX, CL, destinationIsIndirect: true);
                    break;
                case ILOpCode.Code.Stind_I2:
                    XS.Pop(ECX);
                    XS.Pop(EAX);
                    XS.Set(EAX, CX, destinationIsIndirect: true);
                    break;
                case ILOpCode.Code.Stind_I:
                case ILOpCode.Code.Stind_I4:
                case ILOpCode.Code.Stind_R4:
                    XS.Pop(ECX);
                    XS.Pop(EAX);
                    XS.Set(EAX, ECX, destinationIsIndirect: true);
                    break;
                case ILOpCode.Code.Stind_I8:
                case ILOpCode.Code.Stind_R8:
                case ILOpCode.Code.Stind_Ref:
                    XS.Pop(ECX);
                    XS.Pop(EBX);
                    XS.Pop(EAX);
                    XS.Set(EAX, ECX, destinationIsIndirect: true);
                    XS.Set(EAX, EBX, destinationDisplacement: 4);
                    break;
            }
        }
    }
}
