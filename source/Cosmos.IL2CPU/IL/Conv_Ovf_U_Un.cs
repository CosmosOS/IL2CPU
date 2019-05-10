namespace Cosmos.IL2CPU.X86.IL
{
	[Cosmos.IL2CPU.OpCode(ILOpCode.Code.Conv_Ovf_U_Un)]
	public class Conv_Ovf_U_Un: ILOp
	{
		public Conv_Ovf_U_Un(XSharp.Assembler.Assembler aAsmblr):base(aAsmblr)
		{
		}

		public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode) {
			var xSource = aOpCode.StackPopTypes[0];
			Conv_U4.DoExecute(SizeOfType(xSource), TypeIsFloat(xSource), false, true, Assembler, aMethod, aOpCode);

		}
	}
}
