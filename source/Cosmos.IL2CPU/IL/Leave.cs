using Cosmos.IL2CPU.Extensions;
using IL2CPU.Reflection;

using XSharp;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Leave)]
    public class Leave : ILOp
    {
        public Leave(XSharp.Assembler.Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            // apparently, Roslyn changed something to the output. We now have to figure out where to jump to.
            if (aOpCode.CurrentExceptionRegion.Kind.HasFlag(ExceptionBlockKind.Finally)
                && aOpCode.CurrentExceptionRegion.HandlerOffset > aOpCode.Position)
            {
                XS.Set(aMethod.MethodInfo.GetFullName() + "_" + "LeaveAddress_" + aOpCode.CurrentExceptionRegion.HandlerOffset.ToString("X2"), Assembler.CurrentIlLabel + "." + (Assembler.AsmIlIdx + 2).ToString("X2"), destinationIsIndirect: true, size: RegisterSize.Int32);
                XS.Jump(AppAssembler.TmpPosLabel(aMethod, aOpCode.CurrentExceptionRegion.HandlerOffset));
            }

            XS.Jump(AppAssembler.TmpBranchLabel(aMethod, aOpCode));
        }
    }
}
