namespace Cosmos.IL2CPU.IL
{
	[global::Cosmos.IL2CPU.OpCode(ILOpCode.Code.Conv_Ovf_U2_Un)]
	public class Conv_Ovf_U2_Un: ILOp
	{
		public Conv_Ovf_U2_Un(XSharp.Assembler.Assembler aAsmblr):base(aAsmblr)
		{
		}

		public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode) {
			var xSource = aOpCode.StackPopTypes[0];
			var xSourceSize = SizeOfType(xSource);
			var xSourceIsFloat = TypeIsFloat(xSource);
			Conv_U2.DoExecute(xSourceSize, xSourceIsFloat, false, true, Assembler, aMethod, aOpCode);

		}
	}
}
