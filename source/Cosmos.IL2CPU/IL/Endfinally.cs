
using Cosmos.IL2CPU.Extensions;
using XSharp.Common;
using CPUx86 = XSharp.Assembler.x86;
using static XSharp.Common.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Endfinally)]
    public class Endfinally : ILOp
    {
        public Endfinally(XSharp.Assembler.Assembler aAsmblr) : base(aAsmblr)
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            XS.DataMember(aMethod.MethodBase.GetFullName() + "_" + "LeaveAddress_" + aOpCode.CurrentExceptionRegion.HandlerOffset.ToString("X2"), 0);
            XS.Set(EAX, aMethod.MethodBase.GetFullName() + "_" + "LeaveAddress_" + aOpCode.CurrentExceptionRegion.HandlerOffset.ToString("X2"));
            new CPUx86.Jump { DestinationReg = EAX, DestinationIsIndirect = true };
        }
    }
}
