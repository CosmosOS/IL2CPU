using Cosmos.IL2CPU.CIL.Utils;

namespace Cosmos.IL2CPU.CIL.Instructions
{
	public class Conv_Ovf_U1_Un: ILOp
	{
		public Conv_Ovf_U1_Un(XSharp.Assembler.Assembler aAsmblr):base(aAsmblr)
		{
		}

		public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode) {
			var xSource = aOpCode.StackPopTypes[0];
			Conv_U1.DoExecute(TypeIsFloat(xSource), SizeOfType(xSource), false, true, Assembler, aMethod, aOpCode);

		}
	}
}