using System;
using Cosmos.IL2CPU.ILOpCodes;
using CPUx86 = XSharp.Assembler.x86;
using CPU = XSharp.Assembler;
using XSharp.Assembler;

using XSharp.Common;

namespace Cosmos.IL2CPU.X86.IL
{
    [Cosmos.IL2CPU.OpCode( ILOpCode.Code.Ldftn )]
    public class Ldftn : ILOp
    {
        public Ldftn( XSharp.Assembler.Assembler aAsmblr )
            : base( aAsmblr )
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode )
        {
          XS.Push(LabelName.Get(((OpMethod)aOpCode).Value));
        }
    }
}
