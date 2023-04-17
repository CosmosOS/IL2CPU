using System;
using Cosmos.IL2CPU.ILOpCodes;
using XSharp;
using XSharp.Assembler;

namespace Cosmos.IL2CPU.IL
{
    [OpCode(ILOpCode.Code.Ldc_R8)]
    public class Ldc_R8 : ILOp
    {
        public Ldc_R8(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xBytes = BitConverter.GetBytes(((OpDouble)aOpCode).Value);

            XS.Push (BitConverter.ToUInt32(xBytes, 4));
            XS.Push (BitConverter.ToUInt32(xBytes, 0));
        }
    }
}
