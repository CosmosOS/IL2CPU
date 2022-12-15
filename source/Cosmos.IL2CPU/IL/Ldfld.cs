using System;

using IL2CPU.API;
using Cosmos.IL2CPU.ILOpCodes;

using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    /// <summary>
    /// Finds the value of a field in the object whose reference is currently on the evaluation stack.
    /// </summary>
    /// <remarks>
    /// MSDN:
    /// The stack transitional behavior, in sequential order, is:
    /// 1. An object reference (or pointer) is pushed onto the stack.
    /// 2. The object reference (or pointer) is popped from the stack; the value of the specified field in the object is found.
    /// 3. The value stored in the field is pushed onto the stack.
    /// The ldfld instruction pushes the value of a field located in an object onto the stack.
    /// The object must be on the stack as an object reference (type O), a managed pointer (type &),
    /// an unmanaged pointer (type native int), a transient pointer (type *), or an instance of a value type.
    /// The use of an unmanaged pointer is not permitted in verifiable code.
    /// The object's field is specified by a metadata token that must refer to a field member.
    /// The return type is the same as the one associated with the field. The field may be either an instance field
    /// (in which case the object must not be a null reference) or a static field.
    ///
    /// The ldfld instruction can be preceded by either or both of the Unaligned and Volatile prefixes.
    ///
    /// NullReferenceException is thrown if the object is null and the field is not static.
    ///
    /// MissingFieldException is thrown if the specified field is not found in the metadata.
    ///
    /// This is typically checked when Microsoft Intermediate Language (MSIL) instructions are converted to native code, not at run time.
    /// </remarks>
    [OpCode(ILOpCode.Code.Ldfld)]
    public class Ldfld : ILOp
    {
        public Ldfld(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xOpCode = (OpField)aOpCode;
            var xStackType = aOpCode.StackPopTypes[0];

            var xFieldInfo = ResolveField(xOpCode.Value);
            var xDeclaringType = xFieldInfo.DeclaringType;
            var xFieldType = xFieldInfo.FieldType;
            var xOffset = GetFieldOffset(xFieldInfo);

            XS.Comment("Field: " + xFieldInfo.Id);
            XS.Comment("Type: " + xFieldInfo.FieldType.ToString());
            XS.Comment("Size: " + xFieldInfo.Size);
            XS.Comment("DeclaringType: " + xDeclaringType.FullName);
            XS.Comment("TypeOnStack: " + xStackType.FullName);
            XS.Comment("Offset: " + xOffset + " (includes object header)");

            if (xDeclaringType.IsValueType && MemberInfoComparer.Instance.Equals(xDeclaringType, xStackType))
            {
                var xDeclaringTypeStackSize = Align(SizeOfType(xDeclaringType), 4);
                var xFieldSize = xFieldInfo.Size;
                var xStackOffset = (int)(-xDeclaringTypeStackSize + xOffset + xFieldSize - 4);

                XS.Add(ESP, xDeclaringTypeStackSize);

                if ((xFieldInfo.Size < 4 && IsIntegerBasedType(xFieldType))
                    || xFieldType == typeof(bool)
                    || xFieldType == typeof(char))
                {
                    if (TypeIsSigned(xFieldType))
                    {
                        XS.MoveSignExtend(EAX, ESP, sourceDisplacement: xStackOffset + (4 - (int)xFieldSize), size: (RegisterSize)(8 * xFieldSize));
                        XS.Push(EAX);
                    }
                    else
                    {
                        XS.MoveZeroExtend(EAX, ESP, sourceDisplacement: xStackOffset + (4 - (int)xFieldSize), size: (RegisterSize)(8 * xFieldSize));
                        XS.Push(EAX);
                    }

                    return;
                }

                for (int i = 0; i < xFieldSize / 4; i++)
                {
                    XS.Push(ESP, displacement: xStackOffset);
                }

                switch (xFieldSize % 4)
                {
                    case 0:
                        break;
                    case 1:
                        XS.Xor(EAX, EAX);
                        XS.Set(AL, ESP, sourceDisplacement: xStackOffset + 3);
                        XS.Push(EAX);
                        break;
                    case 2:
                        XS.Xor(EAX, EAX);
                        XS.Set(AX, ESP, sourceDisplacement: xStackOffset + 2);
                        XS.Push(EAX);
                        break;
                    case 3:
                        XS.Xor(EAX, EAX);
                        XS.Set(AX, ESP, sourceDisplacement: xStackOffset + 2);
                        XS.ShiftLeft(EAX, 4);
                        XS.Set(AL, ESP, sourceDisplacement: xStackOffset + 1);
                        XS.Push(EAX);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                return;
            }

            // pushed size is always 4 or 8
            var xSize = xFieldInfo.Size;
            if (IsReferenceType(xStackType))
            {
                DoNullReferenceCheck(Assembler, DebugEnabled, 4);
                XS.Add(ESP, 4);
            }
            else
            {
                DoNullReferenceCheck(Assembler, DebugEnabled, 0);
            }
            XS.Pop(ECX);

            XS.Add(ECX, (uint)xOffset);

            if (xFieldInfo.IsExternalValue)
            {
                XS.Set(ECX, ECX, sourceIsIndirect: true);
            }

            for (int i = 1; i <= xSize / 4; i++)
            {
                XS.Set(EAX, ECX, sourceDisplacement: (int)(xSize - i * 4));
                XS.Push(EAX);
            }

            if(xSize % 4 != 0)
            {
                XS.Set(EAX, 0);
            }

            switch (xSize % 4)
            {
                case 1:
                    XS.Set(AL, ECX, sourceIsIndirect: true);
                    XS.Push(EAX);
                    break;

                case 2:
                    XS.Set(AX, ECX, sourceIsIndirect: true);
                    XS.Push(EAX);
                    break;

                case 3: //For Release
                    XS.Set(EAX, ECX, sourceIsIndirect: true);
                    XS.ShiftRight(EAX, 8);
                    XS.Push(EAX);
                    break;

                case 0:
                    {
                        break;
                    }
                default:
                    throw new Exception(string.Format("Remainder size {0} {1:D} not supported!", xFieldInfo.FieldType.ToString(), xSize));
            }
        }

        public static int GetFieldOffset(Type aDeclaringType, string aFieldId)
        {
            int xExtraOffset = 0;
            var xFieldInfo = ResolveField(aDeclaringType, aFieldId, true);
            bool xNeedsGC = IsReferenceType(aDeclaringType);
            if (xNeedsGC)
            {
                xExtraOffset = 12;
            }
            return (int)(xExtraOffset + xFieldInfo.Offset);
        }

        public static int GetFieldOffset(_FieldInfo fieldInfo)
        {
            var offset = (int)fieldInfo.Offset;

            if (IsReferenceType(fieldInfo.DeclaringType))
            {
                offset += ObjectUtils.FieldDataOffset;
            }

            return offset;
        }
    }
}
