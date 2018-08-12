using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Conv_U)]
    public class Conv_U : ILOp
    {
        public Conv_U(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            var xSource = aOpCode.StackPopTypes[0];


            if (IsReferenceType(xSource))
            {
                // todo: Stop GC tracking
                XS.Add(ESP, GetNativeIntegerSize());
            }
            else if (IsPointer(xSource))
            {
                // todo: Stop GC tracking
            }
            else
            {
                // todo: for x64, this should be Conv_U8, maybe create a common method for all conv.u ops?
                Conv_U4.DoExecute(SizeOfType(xSource), TypeIsFloat(xSource));
            }
        }
    }
}
