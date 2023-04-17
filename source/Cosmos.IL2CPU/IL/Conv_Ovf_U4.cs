namespace Cosmos.IL2CPU.IL
{
    [OpCode(ILOpCode.Code.Conv_Ovf_U4)]
    public class Conv_Ovf_U4 : ILOp
    {
        public Conv_Ovf_U4(XSharp.Assembler.Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

		public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode) {
			var xSource = aOpCode.StackPopTypes[0];
			Conv_U4.DoExecute(SizeOfType(xSource), TypeIsFloat(xSource), true, true, Assembler, aMethod, aOpCode);
		}
	}
}
