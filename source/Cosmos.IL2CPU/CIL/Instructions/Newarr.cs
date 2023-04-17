using System;
using System.Reflection;
using Cosmos.IL2CPU.CIL.Utils;
using Cosmos.IL2CPU.Cosmos;
using IL2CPU.API;
using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.CIL.Instructions
{
    /// <summary>
    /// Pushes an object reference to a new zero-based, one-dimensional array whose elements are of a specific type onto the evaluation stack.
    /// </summary>
    [OpCode(ILOpCode.Code.Newarr)]
    public class Newarr : ILOp
    {
        public Newarr(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xType = (ILOpCodes.OpType)aOpCode;
            uint xSize = SizeOfType(xType.Value);

            string xTypeID = GetTypeIDLabel(xType.Value);
            MethodBase xCtor = typeof(Array).GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)[0];
            string xCtorName = LabelName.Get(xCtor);

            XS.Comment("Element Size = " + xSize);
            XS.Pop(EAX); // element count
            XS.Push(EAX);
            XS.Set(EDX, xSize);
            XS.Multiply(EDX); // total element size
            XS.Add(EAX, ObjectUtils.FieldDataOffset + 4); // total array size
            XS.Push(EAX);
            XS.Call(LabelName.Get(GCImplementationRefs.AllocNewObjectRef));
            XS.Label(".AfterAlloc");
            XS.Pop(EAX); // location
            XS.Pop(ESI); // element count
            XS.Push(EAX);
            XS.Push(ESP, isIndirect: true);
            XS.Push(ESP, isIndirect: true);
            // it's on the stack 3 times now, once from the return value, twice from the pushes;

            XS.Pop(EAX);
            XS.Set(EBX, xTypeID, sourceIsIndirect: true);  // array type id
            XS.Set(EAX, EBX, destinationIsIndirect: true); // array type id
            XS.Set(EAX, (uint)ObjectUtils.InstanceTypeEnum.Array, destinationDisplacement: 4, destinationIsIndirect: true);
            XS.Set(EAX, ESI, destinationDisplacement: 8, destinationIsIndirect: true); // element count
            XS.Set(EAX, xSize, destinationDisplacement: 12, destinationIsIndirect: true); // element size
            XS.Push(0);
            XS.Call(xCtorName);
            XS.Push(0);
        }
    }
}
