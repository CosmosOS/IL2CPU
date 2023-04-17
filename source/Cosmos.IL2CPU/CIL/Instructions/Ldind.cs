using Cosmos.IL2CPU.CIL.Utils;
using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.CIL.Instructions
{
    public class Ldind : ILOp
    {
        public Ldind(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            DoNullReferenceCheck(Assembler, DebugEnabled, 0);

            XS.Pop(EAX);

            switch (aOpCode.OpCode)
            {
                case ILOpCode.Code.Ldind_I1:
                    XS.MoveSignExtend(EAX, EAX, sourceIsIndirect: true, size: RegisterSize.Byte8);
                    XS.Push(EAX);
                    break;
                case ILOpCode.Code.Ldind_I2:
                    XS.MoveSignExtend(EAX, EAX, sourceIsIndirect: true, size: RegisterSize.Short16);
                    XS.Push(EAX);
                    break;
                case ILOpCode.Code.Ldind_U1:
                    XS.MoveZeroExtend(EAX, EAX, sourceIsIndirect: true, size: RegisterSize.Byte8);
                    XS.Push(EAX);
                    break;
                case ILOpCode.Code.Ldind_U2:
                    XS.MoveZeroExtend(EAX, EAX, sourceIsIndirect: true, size: RegisterSize.Short16);
                    XS.Push(EAX);
                    break;
                case ILOpCode.Code.Ldind_I:
                case ILOpCode.Code.Ldind_I4:
                case ILOpCode.Code.Ldind_U4:
                case ILOpCode.Code.Ldind_R4:
                    XS.Push(EAX, isIndirect: true);
                    break;
                case ILOpCode.Code.Ldind_I8:
                case ILOpCode.Code.Ldind_R8:
                case ILOpCode.Code.Ldind_Ref:
                    XS.Push(EAX, displacement: 4);
                    XS.Push(EAX, isIndirect: true);
                    break;
            }
        }
    }
}
