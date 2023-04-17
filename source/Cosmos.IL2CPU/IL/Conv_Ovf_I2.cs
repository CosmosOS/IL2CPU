namespace Cosmos.IL2CPU.IL
{
	[global::Cosmos.IL2CPU.OpCode(ILOpCode.Code.Conv_Ovf_I2)]
	public class Conv_Ovf_I2: ILOp
	{
		public Conv_Ovf_I2(XSharp.Assembler.Assembler aAsmblr):base(aAsmblr)
		{
		}

		public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
		{
			var xSource = aOpCode.StackPopTypes[0];
			Conv_I2.DoExecute(SizeOfType(xSource), TypeIsFloat(xSource), true, true, Assembler, aMethod, aOpCode);
		}
	}
}
