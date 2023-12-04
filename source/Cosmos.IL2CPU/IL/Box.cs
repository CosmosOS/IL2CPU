using Cosmos.IL2CPU.ILOpCodes;
using IL2CPU.API;
using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Box)]
    public class Box : ILOp
    {
        public Box(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xType = (OpType)aOpCode;

            if (IsReferenceType(xType.Value))
            {
                return;
            }

            uint xSize = Align(SizeOfType(xType.Value), 4);
            string xTypeID = GetTypeIDLabel(xType.Value);

            XS.Push(ObjectUtils.FieldDataOffset + xSize);
            XS.Call(LabelName.Get(GCImplementationRefs.AllocNewObjectRef));
            XS.Pop(RSI);
            XS.Set(RBX, xTypeID, sourceIsIndirect: true);
            XS.Set(RSI, RBX, destinationIsIndirect: true);
            XS.Set(RSI, (uint)ObjectUtils.InstanceTypeEnum.BoxedValueType, destinationDisplacement: 4, size: RegisterSize.Long64);
            new Comment(Assembler, "xSize is " + xSize);
            for (int i = 0; i < xSize / 4; i++)
            {
                XS.Pop(RDX);
                XS.Set(RSI, RDX, destinationDisplacement: ObjectUtils.FieldDataOffset + i * 4, size: RegisterSize.Long64);
            }
            XS.Push(RSI);
            XS.Push(0);
        }
    }
}
