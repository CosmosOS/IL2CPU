using Cosmos.IL2CPU.CIL.Utils;

namespace Cosmos.IL2CPU.CIL.Instructions
{
  public class Conv_Ovf_I4 : ILOp
  {
    public Conv_Ovf_I4(XSharp.Assembler.Assembler aAsmblr)
      : base(aAsmblr)
    {
    }

    public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
    {
      var xSource = aOpCode.StackPopTypes[0];
      Conv_I4.DoExecute(SizeOfType(xSource), TypeIsFloat(xSource), true, true, Assembler, aMethod, aOpCode);
    }
  }
}
