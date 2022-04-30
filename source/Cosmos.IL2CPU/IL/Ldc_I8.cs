using System;

using Cosmos.IL2CPU.ILOpCodes;

using XSharp;
using XSharp.Assembler;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Ldc_I8)]
    public class Ldc_I8 : ILOp
    {
        public Ldc_I8(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xBytes = BitConverter.GetBytes(((OpInt64)aOpCode).Value);
            // push high part
            XS.Push(BitConverter.ToUInt32(xBytes, 4));
            // push low part
            XS.Push(BitConverter.ToUInt32(xBytes, 0));
        }
    }
}
