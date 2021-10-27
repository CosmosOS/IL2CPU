using System;
using System.Linq;
using System.Reflection;
using Cosmos.IL2CPU.Extensions;
using XSharp;
using CPUx86 = XSharp.Assembler.x86;
using static XSharp.XSRegisters;
using CPU = XSharp.Assembler.x86;
using IL2CPU.API;

namespace Cosmos.IL2CPU.X86.IL
{
    [Cosmos.IL2CPU.OpCode(ILOpCode.Code.Stfld)]
    public class Stfld : ILOp
    {
        static int ID = 10;

        public Stfld(XSharp.Assembler.Assembler aAsmblr) : base(aAsmblr)
        {
        }

        public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            var xOpCode = (ILOpCodes.OpField)aOpCode;
            var xField = xOpCode.Value;
            XS.Comment("Operand type: " + aOpCode.StackPopTypes[1].ToString());
            DoExecute(Assembler, aMethod, xField, DebugEnabled, IsReferenceType(aOpCode.StackPopTypes[1]));
        }

        public static void DoExecute(XSharp.Assembler.Assembler aAssembler, _MethodInfo aMethod, string aFieldId, Type aDeclaringObject, bool aNeedsGC, bool debugEnabled)
        {
            var xFieldInfo = ResolveField(aDeclaringObject, aFieldId, true);
            var xActualOffset = Ldfld.GetFieldOffset(aDeclaringObject, aFieldId);
            var xSize = xFieldInfo.Size;
            XS.Comment("DeclaringObject: " + aDeclaringObject.Name);
            XS.Comment("Field: " + xFieldInfo.Id);
            var fieldType = xFieldInfo.FieldType;
            XS.Comment("Type: " + fieldType.ToString());
            XS.Comment("Size: " + xFieldInfo.Size);
            XS.Comment("Offset: " + xActualOffset + " (includes object header)");
            XS.Comment("ID: " + (++ID).ToString("X"));

            uint xRoundedSize = Align(xSize, 4);
            if (aNeedsGC)
            {
                DoNullReferenceCheck(aAssembler, debugEnabled, (int)xRoundedSize + 4);
            }
            else
            {
                DoNullReferenceCheck(aAssembler, debugEnabled, (int)xRoundedSize);
            }

            XS.Comment("After Nullref check");

            // Determine field in obejct position
            if (aNeedsGC)
            {
                XS.Set(ECX, ESP, sourceDisplacement: (int)xRoundedSize + 4);
            }
            else
            {
                XS.Set(ECX, ESP, sourceDisplacement: (int)xRoundedSize);
            }

            if (xActualOffset != 0)
            {
                XS.Add(ECX, (uint)(xActualOffset));
            }

            // Notify GC if necessary
            GCUpdateOldObject(aMethod, xSize, fieldType, ID);

            //TODO: Can't we use an x86 op to do a byte copy instead and be faster?
            for (int i = 0; i < (xSize / 4); i++)
            {
                XS.Pop(EAX);
                XS.Set(ECX, EAX, destinationDisplacement: (int)((i * 4)));
            }

            switch (xSize % 4)
            {
                case 1:
                    {
                        XS.Pop(EAX);
                        new CPUx86.Mov { DestinationReg = CPUx86.RegistersEnum.ECX, DestinationIsIndirect = true, DestinationDisplacement = (int)((xSize / 4) * 4), SourceReg = CPUx86.RegistersEnum.AL };
                        break;
                    }
                case 2:
                    {
                        XS.Pop(EAX);
                        new CPUx86.Mov { DestinationReg = CPUx86.RegistersEnum.ECX, DestinationIsIndirect = true, DestinationDisplacement = (int)((xSize / 4) * 4), SourceReg = CPUx86.RegistersEnum.AX };
                        break;
                    }
                case 3:
                    {
                        XS.Pop(EAX);
                        // move 2 lower bytes
                        new CPUx86.Mov { DestinationReg = CPUx86.RegistersEnum.ECX, DestinationIsIndirect = true, DestinationDisplacement = (int)((xSize / 4) * 4), SourceReg = CPUx86.RegistersEnum.AX };
                        // shift third byte to lowest
                        XS.ShiftRight(EAX, 16);
                        new CPUx86.Mov { DestinationReg = CPUx86.RegistersEnum.ECX, DestinationIsIndirect = true, DestinationDisplacement = (int)((xSize / 4) * 4) + 2, SourceReg = CPUx86.RegistersEnum.AL };
                        break;
                    }
                case 0:
                    {
                        break;
                    }
                default:
                    throw new Exception("Remainder size " + (xSize % 4) + " not supported!");
            }

            // Notify GC if necessary of new object
            GCUpdateNewObject(aMethod, xSize, fieldType);

            // remove object from stack
            XS.Add(ESP, 4);
            if (aNeedsGC)
            {
                XS.Add(ESP, 4);
            }
        }

        /// <summary>
        /// Increments object reference count of object with address at ECX + 4
        /// </summary>
        /// <param name="aMethod"></param>
        /// <param name="aDeclaringObject"></param>
        /// <param name="xSize"></param>
        /// <param name="fieldType"></param>
        public static void GCUpdateNewObject(_MethodInfo aMethod, uint xSize, Type fieldType, bool aDebug = false)
        {
            if (IsReferenceType(fieldType) && aMethod.UseGC)
            {
                if (xSize != 8)
                {
                    throw new NotImplementedException();
                }
                if (aDebug)
                {
                    XS.Exchange(BX, BX);
                }

                XS.Compare(ECX, 0, destinationIsIndirect: true, destinationDisplacement: 4);
                XS.Jump(CPU.ConditionalTestEnum.Equal, ".SecondAfterGC");

                XS.Push(ECX, isIndirect: true, displacement: 4); // push object as pointer to send to IncRefCount

                XS.Call(LabelName.Get(GCImplementationRefs.IncRefCountRef));

                XS.Label(".SecondAfterGC");
            }
            else if (!fieldType.IsPointer && !fieldType.IsPrimitive && !fieldType.IsPrimitive
                        && !fieldType.IsEnum && aMethod.UseGC)
            {
                //XS.Exchange(BX, BX);
                // let clean up object deal with it
                XS.Push(ECX);
                XS.Push(GetTypeIDLabel(fieldType), isIndirect: true);
                XS.Call(LabelName.Get(GCImplementationRefs.IncStructFieldReferencesRef));
            }
        }

        /// <summary>
        /// Decrements reference count of object with address in ECX + 4
        /// </summary>
        /// <param name="aMethod"></param>
        /// <param name="xSize"></param>
        /// <param name="fieldType"></param>
        public static void GCUpdateOldObject(_MethodInfo aMethod, uint xSize, Type fieldType, int id, bool debug = false)
        {
            if (IsReferenceType(fieldType) && aMethod.UseGC)
            {
                if (xSize != 8)
                {
                    throw new NotImplementedException();
                }
                if (debug)
                {
                    XS.Exchange(BX, BX);
                }

                XS.Compare(ECX, 0, destinationIsIndirect: true, destinationDisplacement: 4);
                XS.Jump(CPU.ConditionalTestEnum.Equal, ".AfterGC");
                XS.Push(ECX); // the call will trash all registers, so store it on the stack
                XS.Push(ECX, isIndirect: true, displacement: 4); // push object as pointer/uint to send to DecTypedRefCount
                XS.Push(id);
                XS.Call(LabelName.Get(GCImplementationRefs.DecRefCountRef));
                XS.Pop(ECX); // restore ecx
                XS.Label(".AfterGC");
            }
            else if (!fieldType.IsPointer && !fieldType.IsPrimitive && !fieldType.IsPrimitive
                        && !fieldType.IsEnum && aMethod.UseGC)
            {
                //XS.Exchange(BX, BX);
                // let clean up object deal with it
                XS.Push(ECX); // the call will trash all registers, so store it on the stack
                XS.Push(ECX, isIndirect: true);
                XS.Push(GetTypeIDLabel(fieldType), isIndirect: true);
                XS.Call(LabelName.Get(GCImplementationRefs.CleanupTypedObjectRef));
                XS.Pop(ECX);
            }
        }

        public static void DoExecute(XSharp.Assembler.Assembler aAssembler, _MethodInfo aMethod, FieldInfo aField, bool debugEnabled, bool aNeedsGC)
        {
            DoExecute(aAssembler, aMethod, aField.GetFullName(), aField.DeclaringType, aNeedsGC, debugEnabled);
        }

    }
}
