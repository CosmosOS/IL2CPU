using System;
using System.Reflection;

using IL2CPU.API;
using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
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
            XS.Pop(RAX); // element count
            XS.Push(RAX);
            XS.Set(RDX, xSize);
            XS.Multiply(RDX); // total element size
            XS.Add(RAX, ObjectUtils.FieldDataOffset + 4); // total array size
            XS.Push(RAX);
            XS.Call(LabelName.Get(GCImplementationRefs.AllocNewObjectRef));
            XS.Label(".AfterAlloc");
            XS.Pop(RAX); // location
            XS.Pop(RSI); // element count
            XS.Push(RAX);
            XS.Push(RSP, isIndirect: true);
            XS.Push(RSP, isIndirect: true);
            // it's on the stack 3 times now, once from the return value, twice from the pushes;

            XS.Pop(RAX);
            XS.Set(RBX, xTypeID, sourceIsIndirect: true);  // array type id
            XS.Set(RAX, RBX, destinationIsIndirect: true); // array type id
            XS.Set(RAX, (uint)ObjectUtils.InstanceTypeEnum.Array, destinationDisplacement: 4, destinationIsIndirect: true);
            XS.Set(RAX, RSI, destinationDisplacement: 8, destinationIsIndirect: true); // element count
            XS.Set(RAX, xSize, destinationDisplacement: 12, destinationIsIndirect: true); // element size
            XS.Push(0);
            XS.Call(xCtorName);
            XS.Push(0);
        }
    }
}
