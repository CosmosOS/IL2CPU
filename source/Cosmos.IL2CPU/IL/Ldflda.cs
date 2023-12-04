using System;
using Cosmos.IL2CPU.Extensions;
using XSharp;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    [Cosmos.IL2CPU.OpCode(ILOpCode.Code.Ldflda)]
    public class Ldflda : ILOp
    {
        public Ldflda(XSharp.Assembler.Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xOpCode = (ILOpCodes.OpField)aOpCode;
            DoExecute(Assembler, aMethod, xOpCode.Value.DeclaringType, xOpCode.Value.GetFullName(), true, DebugEnabled, aOpCode.StackPopTypes[0]);
        }

        public static void DoExecute(XSharp.Assembler.Assembler Assembler, Il2cpuMethodInfo aMethod, Type aDeclaringType, string aField, bool aDerefValue, bool aDebugEnabled, Type aTypeOnStack)
        {
            var xFieldInfo = ResolveField(aDeclaringType, aField, true);
            DoExecute(Assembler, aMethod, aDeclaringType, xFieldInfo, aDerefValue, aDebugEnabled, aTypeOnStack);
        }

        public static void DoExecute(XSharp.Assembler.Assembler Assembler, Il2cpuMethodInfo aMethod, Type aDeclaringType, _FieldInfo aField, bool aDerefValue, bool aDebugEnabled, Type aTypeOnStack)
        {
            XS.Comment("Field: " + aField.Id);
            int xExtraOffset = 0;

            bool xNeedsGC = aDeclaringType.IsClass && !aDeclaringType.IsValueType;

            if (xNeedsGC)
            {
                xExtraOffset = 12;
            }

            if (!aTypeOnStack.IsPointer && aDeclaringType.IsClass)
            {
                DoNullReferenceCheck(Assembler, aDebugEnabled, 4);
                XS.Add(RSP, 4);
            }
            else
            {
                DoNullReferenceCheck(Assembler, aDebugEnabled, 0);
            }

            if (aDerefValue && aField.IsExternalValue)
            {
                XS.Set(RSP, RAX, destinationIsIndirect: true);
            }
            else
            {
                XS.Pop(RAX);
                if (aDeclaringType.Name == "RawArrayData" && aField.Field.Name == "Data")
                {
                    // if we accidently load 64bit assemblies, we get an incorrect extra 4 bytes of offset, so we just hardcode the offset
                    XS.Add(RAX, (uint)(4 + xExtraOffset));
                }
                else
                {
                    XS.Add(RAX, (uint)(aField.Offset + xExtraOffset));
                }
                XS.Push(RAX);
            }
        }
    }
}
