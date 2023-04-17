using System;
using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.IL
{
    [OpCode(ILOpCode.Code.Conv_I)]
    public class Conv_I : ILOp
    {
        public Conv_I(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xSource = aOpCode.StackPopTypes[0];

            if (IsReferenceType(xSource))
            {
                // todo: Stop GC tracking
                XS.Add(ESP, SizeOfType(typeof(IntPtr)));
            }
            else if (IsPointer(xSource))
            {
                // todo: Stop GC tracking
            }
            else
            {
                // todo: for x64, this should be Conv_I8, maybe create a common method for all conv.i ops?
                Conv_I4.DoExecute(SizeOfType(xSource), TypeIsFloat(xSource), TypeIsSigned(xSource), false, Assembler, aMethod, aOpCode);
            }
        }
    }
}
