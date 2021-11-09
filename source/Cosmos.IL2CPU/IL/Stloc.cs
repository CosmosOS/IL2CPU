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

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            var xOpVar = (OpVar)aOpCode;
            var xVar = aMethod.MethodBase.GetLocalVariables()[xOpVar.Value];
            var xStackCount = (int)GetStackCountForLocal(aMethod, xVar.LocalType);
            var xEBPOffset = (int)GetEBPOffsetForLocal(aMethod, xOpVar.Value);
            var xSize = SizeOfType(xVar.LocalType);

            XS.Comment("Local type = " + xVar.LocalType);
            XS.Comment("Local EBP offset = " + xEBPOffset);
            XS.Comment("Local size = " + xSize);

            // Notify GC if necessary
            if (IsReferenceType(xVar.LocalType) && aMethod.UseGC)
            {
                if (xSize != 8)
                {
                    throw new NotImplementedException();
                }

                //XS.Exchange(BX, BX);

                XS.Compare(EBP, 0, destinationIsIndirect: true, destinationDisplacement: -xEBPOffset);
                XS.Jump(CPU.ConditionalTestEnum.Equal, ".AfterGC");
                XS.Push(ECX); // the call will trash all registers
                XS.Push(EBP, isIndirect: true, displacement: -xEBPOffset); // push object as pointer/uint to send to DecRefCount
                XS.Push(0);
                XS.Call(LabelName.Get(GCImplementationRefs.DecRefCountRef));
                XS.Pop(ECX); // restore
                XS.Label(".AfterGC");
            }
            else if (!xVar.LocalType.IsPointer && !xVar.LocalType.IsPrimitive && !xVar.LocalType.IsPrimitive && !xVar.LocalType.IsByRef
                        && !xVar.LocalType.IsEnum && aMethod.UseGC)
            {
                //XS.Exchange(BX, BX);
                // let clean up object deal with it
                XS.Push(EBP, isIndirect: true, displacement: -xEBPOffset);
                XS.Push(GetTypeIDLabel(xVar.LocalType), isIndirect: true);
                XS.Call(LabelName.Get(GCImplementationRefs.CleanupTypedObjectRef));
            }

            for (int i = xStackCount - 1; i >= 0; i--)
            {
                XS.Pop(EAX);
                XS.Set(EBP, EAX, destinationDisplacement: 0 - (xEBPOffset + (i * 4)));
            }

            // Notify GC if necessary of new object
            if (IsReferenceType(xVar.LocalType) && aMethod.UseGC)
            {
                if (xSize != 8)
                {
                    throw new NotImplementedException();
                }

                //XS.Exchange(BX, BX);

                XS.Compare(EBP, 0, destinationIsIndirect: true, destinationDisplacement: -xEBPOffset);
                XS.Jump(CPU.ConditionalTestEnum.Equal, ".SecondAfterGC");

                XS.Push(".SecondAfterGC");
                XS.LiteralCode("Call DebugStub_SendSimpleNumber");
                XS.Pop(EAX);

                XS.Push(EBP, isIndirect: true, displacement: -xEBPOffset); // push object as pointer to send to IncRefCount

                XS.Call(LabelName.Get(GCImplementationRefs.IncRefCountRef));

                XS.Label(".SecondAfterGC");
            }
            else if (!xVar.LocalType.IsPointer && !xVar.LocalType.IsPrimitive && !xVar.LocalType.IsPrimitive && !xVar.LocalType.IsByRef
                        && !xVar.LocalType.IsEnum && aMethod.UseGC)
            {
                //XS.Exchange(BX, BX);
                // let clean up object deal with it
                XS.Push(EBP, isIndirect: true, displacement: -xEBPOffset);
                XS.Push(GetTypeIDLabel(xVar.LocalType), isIndirect: true);
                XS.Call(LabelName.Get(GCImplementationRefs.IncStructFieldReferencesRef));
            }
        }
    }
}
