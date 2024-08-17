using System;
using XSharp;
using Cosmos.IL2CPU.ILOpCodes;
using IL2CPU.API;

namespace Cosmos.IL2CPU.X86.IL
{
	[Cosmos.IL2CPU.OpCode(ILOpCode.Code.Ldvirtftn)]
	public class Ldvirtftn: ILOp
	{
		public Ldvirtftn(XSharp.Assembler.Assembler aAsmblr):base(aAsmblr)
		{
		}

		public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode) {
        	XS.Push(LabelName.Get(((OpMethod)aOpCode).Value));
		}
	}
}
