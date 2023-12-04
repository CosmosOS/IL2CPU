using System;
using System.Reflection;
using Cosmos.IL2CPU.Extensions;
using XSharp;
using CPUx86 = XSharp.Assembler.x86;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Stfld)]
    public class Stfld : ILOp
    {
        public Stfld(XSharp.Assembler.Assembler aAsmblr) : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xOpCode = (ILOpCodes.OpField)aOpCode;
            var xField = xOpCode.Value;
            XS.Comment("Operand type: " + aOpCode.StackPopTypes[1].ToString());
            DoExecute(Assembler, aMethod, xField, DebugEnabled, IsReferenceType(aOpCode.StackPopTypes[1]));
        }

        public static void DoExecute(XSharp.Assembler.Assembler aAssembler, Il2cpuMethodInfo aMethod, string aFieldId, Type aDeclaringObject, bool aNeedsGC, bool debugEnabled)
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
                XS.Set(RCX, RSP, sourceDisplacement: (int)xRoundedSize + 4);
            }
            else
            {
                XS.Set(RCX, RSP, sourceDisplacement: (int)xRoundedSize);
            }

            if (xActualOffset != 0)
            {
                XS.Add(RCX, (uint)xActualOffset);
            }

            //TODO: Can't we use an x86 op to do a byte copy instead and be faster?
            for (int i = 0; i < xSize / 4; i++)
            {
                XS.Pop(RAX);
                XS.Set(RCX, RAX, destinationDisplacement: (int)(i * 4));
            }

            switch (xSize % 4)
            {
                case 1:
                    {
                        XS.Pop(RAX);
                        new CPUx86.Mov { DestinationReg = CPUx86.RegistersEnum.ECX, DestinationIsIndirect = true, DestinationDisplacement = (int)(xSize / 4 * 4), SourceReg = CPUx86.RegistersEnum.AL };
                        break;
                    }
                case 2:
                    {
                        XS.Pop(RAX);
                        new CPUx86.Mov { DestinationReg = CPUx86.RegistersEnum.ECX, DestinationIsIndirect = true, DestinationDisplacement = (int)(xSize / 4 * 4), SourceReg = CPUx86.RegistersEnum.AX };
                        break;
                    }
                case 3:
                    {
                        XS.Pop(RAX);
                        // move 2 lower bytes
                        new CPUx86.Mov { DestinationReg = CPUx86.RegistersEnum.ECX, DestinationIsIndirect = true, DestinationDisplacement = (int)(xSize / 4 * 4), SourceReg = CPUx86.RegistersEnum.AX };
                        // shift third byte to lowest
                        XS.ShiftRight(RAX, 16);
                        new CPUx86.Mov { DestinationReg = CPUx86.RegistersEnum.ECX, DestinationIsIndirect = true, DestinationDisplacement = (int)(xSize / 4 * 4) + 2, SourceReg = CPUx86.RegistersEnum.AL };
                        break;
                    }
                case 0:
                    {
                        break;
                    }
                default:
                    throw new Exception("Remainder size " + xSize % 4 + " not supported!");
            }

            // remove object from stack
            XS.Add(RSP, 4);
            if (aNeedsGC)
            {
                XS.Add(RSP, 4);
            }
        }

        public static void DoExecute(XSharp.Assembler.Assembler aAssembler, Il2cpuMethodInfo aMethod, FieldInfo aField, bool debugEnabled, bool aNeedsGC)
        {
            DoExecute(aAssembler, aMethod, aField.GetFullName(), aField.DeclaringType, aNeedsGC, debugEnabled);
        }

    }
}
