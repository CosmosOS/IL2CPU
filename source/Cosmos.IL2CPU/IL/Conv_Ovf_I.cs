namespace Cosmos.IL2CPU.IL
{
  [global::Cosmos.IL2CPU.OpCode(ILOpCode.Code.Conv_Ovf_I)]
  public class Conv_Ovf_I : ILOp
  {
    public Conv_Ovf_I(XSharp.Assembler.Assembler aAsmblr)
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
