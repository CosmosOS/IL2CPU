using System;

using Cosmos.IL2CPU.ILOpCodes;

using XSharp;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Ldc_I8)]
    public class Ldc_I8 : ILOp
    {
        public Ldc_I8(XSharp.Assembler.Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            var xOp = (OpInt64)aOpCode;
            // push high part
            XS.Push(BitConverter.ToUInt32(BitConverter.GetBytes(xOp.Value), 4));
            // push low part
            XS.Push(BitConverter.ToUInt32(BitConverter.GetBytes(xOp.Value), 0));
        }
    }
}
