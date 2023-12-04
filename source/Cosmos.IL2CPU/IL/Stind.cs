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

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            if (aOpCode.OpCode is ILOpCode.Code.Stind_I8 or ILOpCode.Code.Stind_R8
                || aOpCode.OpCode == ILOpCode.Code.Stind_Ref)
            {
                DoNullReferenceCheck(Assembler, DebugEnabled, 8);
            }
            else
            {
                DoNullReferenceCheck(Assembler, DebugEnabled, 4);
            }

            switch (aOpCode.OpCode)
            {
                case ILOpCode.Code.Stind_I1:
                    XS.Pop(RCX);
                    XS.Pop(RAX);
                    XS.Set(RAX, CL, destinationIsIndirect: true);
                    break;
                case ILOpCode.Code.Stind_I2:
                    XS.Pop(RCX);
                    XS.Pop(RAX);
                    XS.Set(RAX, CX, destinationIsIndirect: true);
                    break;
                case ILOpCode.Code.Stind_I:
                case ILOpCode.Code.Stind_I4:
                case ILOpCode.Code.Stind_R4:
                    XS.Pop(RCX);
                    XS.Pop(RAX);
                    XS.Set(RAX, RCX, destinationIsIndirect: true);
                    break;
                case ILOpCode.Code.Stind_I8:
                case ILOpCode.Code.Stind_R8:
                case ILOpCode.Code.Stind_Ref:
                    XS.Pop(RCX);
                    XS.Pop(RBX);
                    XS.Pop(RAX);
                    XS.Set(RAX, RCX, destinationIsIndirect: true);
                    XS.Set(RAX, RBX, destinationDisplacement: 4);
                    break;
            }
        }
    }
}
