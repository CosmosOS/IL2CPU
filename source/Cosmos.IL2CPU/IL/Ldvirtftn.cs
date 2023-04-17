using System;

namespace Cosmos.IL2CPU.IL
{
	[global::Cosmos.IL2CPU.OpCode(ILOpCode.Code.Ldvirtftn)]
	public class Ldvirtftn: ILOp
	{
		public Ldvirtftn(XSharp.Assembler.Assembler aAsmblr):base(aAsmblr)
		{
		}

    public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode) {
        DoNullReferenceCheck(Assembler, DebugEnabled, 0);
        throw new NotImplementedException();
    }


		// using System;
		// using System.IO;
		//
		//
		// using CPU = XSharp.Assembler.x86;
		//
		// namespace Cosmos.IL2CPU.IL.X86 {
		// 	[XSharp.Assembler.OpCode(OpCodeEnum.Ldvirtftn)]
		// 	public class Ldvirtftn: Op {
		//         private string mNextLabel;
		// 	    private string mCurLabel;
		// 	    private uint mCurOffset;
		// 	    private MethodInformation mMethodInformation;
		// 		public Ldvirtftn(ILReader aReader, MethodInformation aMethodInfo)
		// 			: base(aReader, aMethodInfo) {
		//              mMethodInformation = aMethodInfo;
		// 		    mCurOffset = aReader.Position;
		// 		    mCurLabel = IL.Op.GetInstructionLabel(aReader);
		//             mNextLabel = IL.Op.GetInstructionLabel(aReader.NextPosition);
		// 		}
		// 		public override void DoAssemble() {
		//             EmitNotImplementedException(Assembler, GetServiceProvider(), "Ldvirtftn: This has not been implemented at all yet!", mCurLabel, mMethodInformation, mCurOffset, mNextLabel);
		// 		}
		// 	}
		// }

	}
}
