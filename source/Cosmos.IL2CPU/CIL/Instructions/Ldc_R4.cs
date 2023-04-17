using System;
using Cosmos.IL2CPU.CIL.ILOpCodes;
using XSharp;
using XSharp.Assembler;

namespace Cosmos.IL2CPU.CIL.Instructions
{
    [OpCode(ILOpCode.Code.Ldc_R4)]
    public class Ldc_R4 : ILOp
    {
        public Ldc_R4(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            XS.Push(BitConverter.ToUInt32(BitConverter.GetBytes(((OpSingle)aOpCode).Value), 0));
        }
    }
}
