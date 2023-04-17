namespace Cosmos.IL2CPU.CIL.Instructions
{
	[OpCode(ILOpCode.Code.Conv_Ovf_U4_Un)]
	public class Conv_Ovf_U4_Un: ILOp
	{
		public Conv_Ovf_U4_Un(XSharp.Assembler.Assembler aAsmblr):base(aAsmblr)
		{
		}

		public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode) {
			var xSource = aOpCode.StackPopTypes[0];
			Conv_U4.DoExecute(SizeOfType(xSource), TypeIsFloat(xSource), false, true, Assembler, aMethod, aOpCode);

		}
	}
}
