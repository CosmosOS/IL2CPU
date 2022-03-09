using System;
using Cosmos.IL2CPU.Extensions;
using Cosmos.IL2CPU.ILOpCodes;
using IL2CPU.API;
using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;
using CPU = XSharp.Assembler.x86;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Stloc)]
    public class Stloc : ILOp
    {
        public Stloc(Assembler aAsmblr)
          : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xOpVar = (OpVar)aOpCode;
            var xVar = aMethod.MethodBase.GetLocalVariables()[xOpVar.Value];
            var xStackCount = (int)GetStackCountForLocal(aMethod, xVar.LocalType);
            var xEBPOffset = (int)GetEBPOffsetForLocal(aMethod, xOpVar.Value);
            var xSize = SizeOfType(xVar.LocalType);

            XS.Comment("Local type = " + xVar.LocalType);
            XS.Comment("Local EBP offset = " + xEBPOffset);
            XS.Comment("Local size = " + xSize);

            for (int i = xStackCount - 1; i >= 0; i--)
            {
                XS.Pop(EAX);
                XS.Set(EBP, EAX, destinationDisplacement: 0 - (xEBPOffset + (i * 4)));
            }
        }
    }
}
