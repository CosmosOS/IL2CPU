using Cosmos.IL2CPU.CIL.Utils;

namespace Cosmos.IL2CPU.CIL.Instructions
{
    public class Stelem_I8 : ILOp
    {
        public Stelem_I8( XSharp.Assembler.Assembler aAsmblr )
            : base( aAsmblr )
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode )
        {
          Stelem_Ref.Assemble(Assembler, 8, aMethod, aOpCode, DebugEnabled);
        }


        // using System;
        // using System.IO;
        //
        //
        // using CPU = XSharp.Assembler.x86;
        // using CPUx86 = XSharp.Assembler.x86;
        //
        // namespace Cosmos.IL2CPU.IL.X86 {
        // 	[XSharp.Assembler.OpCode(OpCodeEnum.Stelem_I8)]
        // 	public class Stelem_I8: Op {
        // 		private string mNextLabel;
        // 	    private string mCurLabel;
        // 	    private uint mCurOffset;
        // 	    private MethodInformation mMethodInformation;
        //
        //         public Stelem_I8(ILReader aReader, MethodInformation aMethodInfo)
        // 			: base(aReader, aMethodInfo) {
        //              mMethodInformation = aMethodInfo;
        // 		    mCurOffset = aReader.Position;
        // 		    mCurLabel = IL.Op.GetInstructionLabel(aReader);
        //             mNextLabel = IL.Op.GetInstructionLabel(aReader.NextPosition);
        // 		}
        //
        // 		public override void DoAssemble() {
        //             Stelem_Ref.Assemble(Assembler, 8, GetServiceProvider(), mCurLabel, mMethodInformation, mCurOffset, mNextLabel);
        // 		}
        // 	}
        // }

    }
}