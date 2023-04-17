using System.Reflection.Metadata;
using Cosmos.IL2CPU.Extensions;
using XSharp;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.IL
{
  [OpCode(ILOpCode.Code.Leave)]
  public class Leave : ILOp
  {
    public Leave(XSharp.Assembler.Assembler aAsmblr)
        : base(aAsmblr)
    {
    }

    public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
    {
      string jumpTarget = AppAssembler.TmpBranchLabel(aMethod, aOpCode);

      // apparently, Roslyn changed something to the output. We now have to figure out where to jump to.
      if (aOpCode.CurrentExceptionRegion.Kind.HasFlag(ExceptionRegionKind.Finally)
          && aOpCode.CurrentExceptionRegion.HandlerOffset > aOpCode.Position)
      {
        string destination = $"{aMethod.MethodBase.GetFullName()}_LeaveAddress_{aOpCode.CurrentExceptionRegion.HandlerOffset:X2}";
        string source = AppAssembler.TmpBranchLabel(aMethod, aOpCode);
        XS.Set(destination, source, destinationIsIndirect: true, size: RegisterSize.Int32);
        XS.Jump(AppAssembler.TmpPosLabel(aMethod, aOpCode.CurrentExceptionRegion.HandlerOffset));
      }
      else
      {
        XS.Jump(jumpTarget);
      }
    }
  }
}
