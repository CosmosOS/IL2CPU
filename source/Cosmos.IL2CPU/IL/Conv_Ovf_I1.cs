namespace Cosmos.IL2CPU.IL
{
	[global::Cosmos.IL2CPU.OpCode(ILOpCode.Code.Conv_Ovf_I1)]
	public class Conv_Ovf_I1: ILOp
	{
		public Conv_Ovf_I1(XSharp.Assembler.Assembler aAsmblr):base(aAsmblr)
		{
		}

		public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode) {
			var xSource = aOpCode.StackPopTypes[0];
			var xSourceSize = SizeOfType(xSource);
			var xSourceIsFloat = TypeIsFloat(xSource);
			Conv_I1.DoExecute(xSourceSize, xSourceIsFloat, true, true, Assembler, aMethod, aOpCode);
		}
	}
}
