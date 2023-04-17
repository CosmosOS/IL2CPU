using Cosmos.IL2CPU.CIL.Utils;

namespace Cosmos.IL2CPU.CIL.Instructions
{
	[OpCode(ILOpCode.Code.Conv_Ovf_I1_Un)]
	public class Conv_Ovf_I1_Un: ILOp
	{
		public Conv_Ovf_I1_Un(XSharp.Assembler.Assembler aAsmblr):base(aAsmblr)
		{
		}

		public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode) {
			var xSource = aOpCode.StackPopTypes[0];
			var xSourceSize = SizeOfType(xSource);
			var xSourceIsFloat = TypeIsFloat(xSource);
			Conv_I1.DoExecute(xSourceSize, xSourceIsFloat, false, true, Assembler, aMethod, aOpCode);
		}
	}
}
