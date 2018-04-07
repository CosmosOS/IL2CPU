using System;

using Cosmos.IL2CPU.ILOpCodes;

using XSharp;
using XSharp.Assembler;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Ldtoken)]
    public class Ldtoken : ILOp
    {
        public Ldtoken(Assembler aAsmblr)
          : base(aAsmblr)
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            var xToken = (OpToken)aOpCode;
            string xTokenAddress = null;

            if (xToken.ValueIsType)
            {
                xTokenAddress = ILOp.GetTypeIDLabel(xToken.ValueType);
            }
            if (xToken.ValueIsField)
            {
                xTokenAddress = DataMember.GetStaticFieldName(xToken.ValueField);
            }

            if (String.IsNullOrEmpty(xTokenAddress))
            {
                throw new Exception("Ldtoken not implemented!");
            }

            XS.Push(xTokenAddress);
            XS.Push(0);
        }
    }
}
