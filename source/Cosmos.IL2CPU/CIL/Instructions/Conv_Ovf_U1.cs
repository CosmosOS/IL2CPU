using Cosmos.IL2CPU.CIL.Utils;

namespace Cosmos.IL2CPU.CIL.Instructions
{
	public class Conv_Ovf_U1: ILOp
	{
		//Convert to an unsigned int8(on the stack as int32) and throw an exception if overflow
		public Conv_Ovf_U1(XSharp.Assembler.Assembler aAsmblr):base(aAsmblr)
		{
		}

		public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
		{
			var xSource = aOpCode.StackPopTypes[0];
			var xSourceSize = SizeOfType(xSource);
			var xSourceIsFloat = TypeIsFloat(xSource);
			Conv_U1.DoExecute(xSourceIsFloat, xSourceSize, true, true, Assembler, aMethod, aOpCode);
		}
	}
}
