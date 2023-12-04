
using Cosmos.IL2CPU.Extensions;
using XSharp;
using CPUx86 = XSharp.Assembler.x86;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Endfinally)]
    public class Endfinally : ILOp
    {
        public Endfinally(XSharp.Assembler.Assembler aAsmblr) : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            string leaveAddressVariableName = $"{aMethod.MethodBase.GetFullName()}_LeaveAddress_{aOpCode.CurrentExceptionRegion.HandlerOffset:X2}";
            XS.DataMember(leaveAddressVariableName, 0);
            XS.Set(RAX, leaveAddressVariableName);
            new CPUx86.Jump { DestinationReg = RAX, DestinationIsIndirect = true };
        }
    }
}
