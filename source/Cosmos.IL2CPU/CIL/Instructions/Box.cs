using Cosmos.IL2CPU.CIL.ILOpCodes;
using Cosmos.IL2CPU.Cosmos;
using IL2CPU.API;
using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.CIL.Instructions
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
            XS.Pop(ESI);
            XS.Set(EBX, xTypeID, sourceIsIndirect: true);
            XS.Set(ESI, EBX, destinationIsIndirect: true);
            XS.Set(ESI, (uint)ObjectUtils.InstanceTypeEnum.BoxedValueType, destinationDisplacement: 4, size: RegisterSize.Int32);
            new Comment(Assembler, "xSize is " + xSize);
            for (int i = 0; i < xSize / 4; i++)
            {
                XS.Pop(EDX);
                XS.Set(ESI, EDX, destinationDisplacement: ObjectUtils.FieldDataOffset + i * 4, size: RegisterSize.Int32);
            }
            XS.Push(ESI);
            XS.Push(0);
        }
    }
}
