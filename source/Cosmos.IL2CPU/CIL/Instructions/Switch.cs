using Cosmos.IL2CPU.CIL.Utils;
using XSharp;
using XSharp.Assembler.x86;

namespace Cosmos.IL2CPU.CIL.Instructions
{
    public class Switch : ILOp
    {
        public Switch(XSharp.Assembler.Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            ILOpCodes.OpSwitch OpSw = (ILOpCodes.OpSwitch)aOpCode;
            XS.Pop(XSRegisters.EAX);

            for (int i = 0; i < OpSw.BranchLocations.Length; i++)
            {
                string xDestLabel = AppAssembler.TmpPosLabel(aMethod, OpSw.BranchLocations[i]);

                XS.Compare(XSRegisters.EAX, (uint)i);
                XS.Jump(ConditionalTestEnum.Equal, xDestLabel);
            }
        }
    }
}
