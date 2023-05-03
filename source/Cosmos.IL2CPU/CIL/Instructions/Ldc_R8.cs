using System;
using Cosmos.IL2CPU.CIL.ILOpCodes;
using Cosmos.IL2CPU.CIL.Utils;
using XSharp;
using XSharp.Assembler;

namespace Cosmos.IL2CPU.CIL.Instructions
{
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
