using Cosmos.IL2CPU.CIL.Utils;

namespace Cosmos.IL2CPU.CIL.Instructions
{
	public class Conv_Ovf_I8_Un: ILOp
	{
		public Conv_Ovf_I8_Un(XSharp.Assembler.Assembler aAsmblr):base(aAsmblr)
		{
		}

		public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode) {
			var xSource = aOpCode.StackPopTypes[0];
			var xSourceSize = SizeOfType(xSource);
			var xSourceIsFloat = TypeIsFloat(xSource);
			Conv_Ovf_I8.DoExecute(xSourceSize, false, Assembler, aMethod, aOpCode);
		}
	}
}