using Cosmos.IL2CPU.CIL.ILOpCodes;
using Cosmos.IL2CPU.CIL.Utils;

namespace Cosmos.IL2CPU.CIL.Instructions
{
  public class Ldelem : ILOp
  {
    public Ldelem(XSharp.Assembler.Assembler aAsmblr)
      : base(aAsmblr)
    {
    }

    public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
    {
      var xOpType = (OpType)aOpCode;
      var xSize = SizeOfType(xOpType.Value);
      if (xOpType.Value.IsValueType && !xOpType.Value.IsPrimitive)
      {
        Ldelema.Assemble(Assembler, xOpType, xSize, DebugEnabled, aMethod, aOpCode);
        Ldobj.DoAssemble(xOpType.Value);
        return;
      }
      Ldelem_Ref.Assemble(Assembler, xSize, false, aMethod, aOpCode, DebugEnabled);
    }
  }
}
