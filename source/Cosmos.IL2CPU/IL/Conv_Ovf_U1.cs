namespace Cosmos.IL2CPU.X86.IL
{
	[Cosmos.IL2CPU.OpCode(ILOpCode.Code.Conv_Ovf_U1)]
	public class Conv_Ovf_U1: ILOp
	{
		//Convert to an unsigned int8(on the stack as int32) and throw an exception if overflow
		public Conv_Ovf_U1(XSharp.Assembler.Assembler aAsmblr):base(aAsmblr)
		{
		}

		public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
		{
			var xSource = aOpCode.StackPopTypes[0];
			var xSourceSize = SizeOfType(xSource);
			var xSourceIsFloat = TypeIsFloat(xSource);
			Conv_U1.DoExecute(xSourceIsFloat, xSourceSize, true, true, Assembler, aMethod, aOpCode);
		}
	}
}
