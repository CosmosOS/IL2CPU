using System;
using Cosmos.IL2CPU.CIL.ILOpCodes;
using IL2CPU.API;
using XSharp;
using XSharp.Assembler;

namespace Cosmos.IL2CPU.CIL.Instructions
{
    [OpCode(ILOpCode.Code.Ldtoken)]
    public class Ldtoken : ILOp
    {
        public Ldtoken(Assembler aAsmblr)
          : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xToken = (OpToken)aOpCode;
            string xTokenAddress = null;

            if (xToken.ValueIsType)
            {
                xTokenAddress = ILOp.GetTypeIDLabel(xToken.ValueType);
            }
            if (xToken.ValueIsField)
            {
                xTokenAddress = LabelName.GetStaticFieldName(xToken.ValueField);
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
