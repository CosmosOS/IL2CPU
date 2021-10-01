using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

using Cosmos.IL2CPU.Extensions;
using Cosmos.IL2CPU.X86.IL;

using IL2CPU.API;
using IL2CPU.API.Attribs;
using IL2CPU.Debug.Symbols;

using XSharp;
using XSharp.Assembler;
using CPU = XSharp.Assembler.x86;
using static XSharp.XSRegisters;
using Cosmos.IL2CPU.ILOpCodes;
using IL2CPU.Reflection;

using static IL2CPU.Reflection.BaseTypeSystem;

namespace Cosmos.IL2CPU
{
    public abstract class ILOp
    {
        internal static PlugManager PlugManager;
        protected readonly Assembler Assembler;

        protected ILOp(Assembler aAsmblr)
        {
            Assembler = aAsmblr;
        }

        public bool DebugEnabled;

        // This is called execute and not assemble, as the scanner
        // could be used for other things, profiling, analysis, reporting, etc
        public abstract void Execute(_MethodInfo aMethod, ILOpCode aOpCode);

        public static string GetTypeIDLabel(Type aType)
        {
            return "VMT__TYPE_ID_HOLDER__" + DataMember.FilterStringForIncorrectChars(LabelName.GetFullName(aType) + " ASM_IS__" + aType.Assembly.GetName().Name);
        }

        public static uint Align(uint aSize, uint aAlign)
        {
            uint xSize = aSize;
            if ((xSize % aAlign) != 0)
            {
                xSize += aAlign - (xSize % aAlign);
            }
            return xSize;
        }

        public static int SignedAlign(int aSize, int aAlign)
        {
            int xSize = aSize;
            if ((xSize % aAlign) != 0)
            {
                xSize += aAlign - (xSize % aAlign);
            }
            return xSize;
        }

        public static string GetLabel(_MethodInfo aMethod, ILOpCode aOpCode)
        {
            return GetLabel(aMethod, aOpCode.Position);
        }

        public static string GetLabel(MethodBase aMethod)
        {
            return LabelName.Get(aMethod);
        }

        public static string GetLabel(_MethodInfo aMethod)
        {
            if (aMethod.PluggedMethod != null)
            {
                return "PLUG_FOR___" + GetLabel(aMethod.PluggedMethod.MethodBase);
            }
            return GetLabel(aMethod.MethodBase);
        }

        public static string GetLabel(_MethodInfo aMethod, int aPos)
        {
            return LabelName.Get(GetLabel(aMethod), aPos);
        }

        public override string ToString()
        {
            return GetType().Name;
        }

        protected static void Jump_Exception(_MethodInfo aMethod)
        {
            // todo: port to numeric labels
            XS.Jump(GetLabel(aMethod) + AppAssembler.EndOfMethodLabelNameException);
        }

        protected static void Jump_End(_MethodInfo aMethod)
        {
            XS.Jump(GetLabel(aMethod) + AppAssembler.EndOfMethodLabelNameNormal);
        }

        public static uint GetStackCountForLocal(_MethodInfo aMethod, Type aField)
        {
            var xSize = SizeOfType(aField);
            var xResult = xSize / 4;
            if (xSize % 4 != 0)
            {
                xResult++;
            }
            return xResult;
        }

        public static uint GetEBPOffsetForLocal(_MethodInfo aMethod, int localIndex)
        {
            var xLocalInfos = aMethod.MethodBase.GetLocalVariables();
            uint xOffset = 4;
            for (int i = 0; i < xLocalInfos.Count; i++)
            {
                if (i == localIndex)
                {
                    break;
                }
                var xField = xLocalInfos[i];
                xOffset += GetStackCountForLocal(aMethod, xField.LocalType) * 4;
            }
            return xOffset;
        }

        public static uint GetEBPOffsetForLocalForDebugger(_MethodInfo aMethod, int localIndex)
        {
            // because the memory is read in positive direction, we need to add additional size if greater than 4
            uint xOffset = GetEBPOffsetForLocal(aMethod, localIndex);
            var xLocalInfos = aMethod.MethodBase.GetLocalVariables();
            var xField = xLocalInfos[localIndex];
            xOffset += GetStackCountForLocal(aMethod, xField.LocalType) * 4 - 4;
            return xOffset;
        }

        private static void DoGetFieldsInfo(Type aType, List<_FieldInfo> aFields, bool includeStatic)
        {
            var xCurList = new Dictionary<string, _FieldInfo>();
            var xBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            if (includeStatic)
            {
                xBindingFlags |= BindingFlags.Static;
            }
            var xFields = (from item in aType.GetFields(xBindingFlags)
                           orderby item.Name, item.DeclaringType.ToString()
                           select item).ToArray();
            for (int i = 0; i < xFields.Length; i++)
            {
                var xField = xFields[i];
                // todo: should be possible to have GetFields only return fields from a given type, thus removing need of next statement
                if (xField.DeclaringType != aType)
                {
                    continue;
                }

                string xId = xField.GetFullName();

                var xInfo = new _FieldInfo(xId, SizeOfType(xField.FieldType), aType, xField.FieldType);
                xInfo.IsStatic = xField.IsStatic;
                xInfo.Field = xField;

                var xFieldOffsetAttrib =
                  xField.FetchCustomAttributes<FieldOffsetAttribute>(true).FirstOrDefault();
                if (xFieldOffsetAttrib != null)
                {
                    xInfo.Offset = (uint)xFieldOffsetAttrib.Value;
                }

                aFields.Add(xInfo);
                xCurList.Add(xId, xInfo);
            }

            // now check plugs
            if (PlugManager.PlugFields.TryGetValue(aType, out var xPlugFields))
            {
                foreach (var xPlugField in xPlugFields)
                {
                    if (xCurList.TryGetValue(xPlugField.Key, out var xPluggedField))
                    {
                        // plugfield modifies an already existing field

                        // TODO: improve.
                        if (xPlugField.Value.IsExternalValue)
                        {
                            xPluggedField.IsExternalValue = true;
                            xPluggedField.FieldType = xPluggedField.FieldType.MakePointerType();
                            xPluggedField.Size = 4;
                        }
                    }
                    else
                    {
                        xPluggedField = new _FieldInfo(xPlugField.Value.FieldId, SizeOfType(xPlugField.Value.FieldType), aType,
                          xPlugField.Value.FieldType);
                        aFields.Add(xPluggedField);
                    }
                }
            }

            Type xBase = aType.BaseType;
            if (xBase != null)
            {
                DoGetFieldsInfo(xBase, aFields, includeStatic);
            }
        }

        public static List<_FieldInfo> GetFieldsInfo(Type aType, bool includeStatic)
        {
            var loaded = aType.ReplaceLoad();
            return GetFieldsInfoInt(loaded, includeStatic);
        }

        private static List<_FieldInfo> GetFieldsInfoInt(Type aType, bool includeStatic)
        {
            if (aType.IsValueType)
            {
                var fieldsInfo = GetValueTypeFieldsInfo(aType);

                if (includeStatic)
                {
                    foreach (var field in aType.GetFields(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                    {
                        fieldsInfo.Add(
                            new _FieldInfo(field.GetFullName(), SizeOfType(field.FieldType), aType, field.FieldType));
                    }
                }

                return fieldsInfo;
            }

            var xResult = new List<_FieldInfo>(16);
            DoGetFieldsInfo(aType, xResult, includeStatic);
            xResult.Reverse();
            uint xOffset = 0;
            foreach (var xInfo in xResult)
            {
                if (!xInfo.IsOffsetSet && !xInfo.IsStatic)
                {
                    xInfo.Offset = xOffset;
                    xOffset += xInfo.Size;
                }
            }
            var xDebugInfs = new List<FIELD_INFO>();
            foreach (var xInfo in xResult)
            {
                if (!xInfo.IsStatic)
                {
                    xDebugInfs.Add(new FIELD_INFO()
                    {
                        TYPE = xInfo.FieldType.FullName,
                        OFFSET = (int)xInfo.Offset,
                        NAME = GetNameForField(xInfo),
                    });
                }
            }
            DebugInfo.CurrentInstance.WriteFieldInfoToFile(xDebugInfs);
            List<DebugInfo.Field_Map> xFieldMapping = new List<DebugInfo.Field_Map>();
            GetFieldMapping(xResult, xFieldMapping, aType);
            DebugInfo.CurrentInstance.WriteFieldMappingToFile(xFieldMapping);
            return xResult;
        }

        private static List<_FieldInfo> GetValueTypeFieldsInfo(Type type)
        {
            var structLayoutAttribute = type.StructLayoutAttribute;
            var fieldInfos = new List<_FieldInfo>();

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            switch (structLayoutAttribute.Value)
            {
                case LayoutKind.Auto:
                case LayoutKind.Sequential:
                    var offset = 0;
                    var pack = structLayoutAttribute.Pack;

                    if (pack == 0)
                    {
                        pack = (int)SizeOfType(BaseTypes.IntPtr);
                    }

                    if (fields.Length > 0)
                    {
                        var typeAlignment = Math.Min(pack, fields.Max(f => SizeOfType(f.FieldType)));

                        Array.Sort(fields, (x, y) => x.MetadataToken.CompareTo(y.MetadataToken));

                        foreach (var field in fields)
                        {
                            var fieldSize = SizeOfType(field.FieldType);

                            var fieldAlignment = Math.Min(typeAlignment, fieldSize);
                            offset = (int)Align((uint)offset, (uint)fieldAlignment);

                            var fieldInfo = new _FieldInfo(
                                field.GetFullName(), SizeOfType(field.FieldType), type, field.FieldType);
                            fieldInfo.Offset = (uint)offset;
                            fieldInfo.Field = field;

                            fieldInfos.Add(fieldInfo);

                            offset += (int)fieldSize;
                        }
                    }

                    break;
                case LayoutKind.Explicit:
                    foreach (var field in fields)
                    {
                        var fieldInfo = new _FieldInfo(field.GetFullName(), SizeOfType(field.FieldType), type, field.FieldType);
                        fieldInfo.Offset = (uint)(field.FetchCustomAttribute<FieldOffsetAttribute>()?.Value ?? 0);
                        fieldInfo.Field = field;

                        fieldInfos.Add(fieldInfo);
                    }

                    break;
                default:
                    throw new NotSupportedException();
            }

            return fieldInfos;
        }

        private static void GetFieldMapping(List<_FieldInfo> aFieldInfs, List<DebugInfo.Field_Map> aFieldMapping,
          Type aType)
        {
            var xFMap = new DebugInfo.Field_Map();
            xFMap.TypeName = aType.FullName;
            foreach (var xInfo in aFieldInfs)
            {
                xFMap.FieldNames.Add(GetNameForField(xInfo));
            }
            aFieldMapping.Add(xFMap);
        }

        private static string GetNameForField(_FieldInfo inf)
        {
            // First we need to separate out the
            // actual name of field from the type of the field.
            int loc = inf.Id.IndexOf(' ');
            if (loc >= 0)
            {
                string fName = inf.Id.Substring(loc, inf.Id.Length - loc);
                return inf.DeclaringType.FullName + fName;
            }

            return inf.Id;
        }

        protected static uint GetStorageSize(Type aType)
        {
            if (aType.IsValueType)
            {
                var structLayoutAttribute = aType.StructLayoutAttribute;
                var pack = structLayoutAttribute.Pack;

                if (pack == 0)
                {
                    pack = (int)SizeOfType(BaseTypes.IntPtr);
                }

                var fieldsInfo = GetFieldsInfo(aType, false);

                if (fieldsInfo.Count > 0)
                {
                    var typeAlignment = (uint)Math.Min(fieldsInfo.Max(f => f.Size), pack);

                    return (uint)Math.Max(
                        structLayoutAttribute.Size,
                        Align(fieldsInfo.Max(f => f.Offset + f.Size), typeAlignment));
                }
                else
                {
                    return (uint)Math.Max(structLayoutAttribute.Size, 0);
                }
            }

            return (from item in GetFieldsInfo(aType, false)
                    where !item.IsStatic
                    orderby item.Offset descending
                    select item.Offset + item.Size).FirstOrDefault();
        }

        /// <summary>
        /// Emits cleanup code for when an exception occurred inside a method call.
        /// </summary>
        public static void EmitExceptionCleanupAfterCall(Assembler aAssembler, uint aReturnSize, uint aStackSizeBeforeCall,
          uint aTotalArgumentSizeOfMethod)
        {
            XS.Comment("aStackSizeBeforeCall = " + aStackSizeBeforeCall);
            XS.Comment("aTotalArgumentSizeOfMethod = " + aTotalArgumentSizeOfMethod);
            XS.Comment("aReturnSize = " + aReturnSize);

            if (aReturnSize != 0)
            {
                // at least pop return size:
                XS.Comment("Cleanup return");

                // cleanup result values
                for (int i = 0; i < aReturnSize / 4; i++)
                {
                    XS.Add(ESP, 4);
                }
            }

            if (aStackSizeBeforeCall > (aTotalArgumentSizeOfMethod))
            {
                if (aTotalArgumentSizeOfMethod > 0)
                {
                    var xExtraStack = aStackSizeBeforeCall - aTotalArgumentSizeOfMethod;
                    XS.Comment("Cleanup extra stack");

                    // cleanup result values
                    for (int i = 0; i < xExtraStack / 4; i++)
                    {
                        XS.Add(ESP, 4);
                    }
                }
            }
        }

        public static void EmitExceptionLogic(Assembler aAssembler, _MethodInfo aMethodInfo, ILOpCode aCurrentOpCode,
          bool aDoTest, Action aCleanup, string aJumpTargetNoException = null)
        {
            if (aJumpTargetNoException == null)
            {
                aJumpTargetNoException = GetLabel(aMethodInfo, aCurrentOpCode.NextPosition);
            }
            string xJumpTo = null;
            if (aCurrentOpCode != null && aCurrentOpCode.CurrentExceptionRegion != null)
            {
                // todo add support for nested handlers, see comment in Engine.cs
                //if (!((aMethodInfo.CurrentHandler.HandlerOffset < aCurrentOpOffset) || (aMethodInfo.CurrentHandler.HandlerLength + aMethodInfo.CurrentHandler.HandlerOffset) <= aCurrentOpOffset)) {
                XS.Comment(String.Format("CurrentOffset = {0}, HandlerStartOffset = {1}", aCurrentOpCode.Position,
                  aCurrentOpCode.CurrentExceptionRegion.HandlerOffset));
                if (aCurrentOpCode.CurrentExceptionRegion.HandlerOffset > aCurrentOpCode.Position)
                {
                    switch (aCurrentOpCode.CurrentExceptionRegion.Kind)
                    {
                        case ExceptionRegionKind.Catch:
                        case ExceptionRegionKind.Finally:
                            xJumpTo = GetLabel(aMethodInfo, aCurrentOpCode.CurrentExceptionRegion.HandlerOffset);
                            break;
                        case ExceptionRegionKind.Filter:
                            xJumpTo = GetLabel(aMethodInfo, aCurrentOpCode.CurrentExceptionRegion.FilterOffset);
                            break;

                        case ExceptionRegionKind.Fault:
                        default:
                            {
                                throw new Exception("ExceptionHandlerType '" + aCurrentOpCode.CurrentExceptionRegion.Kind.ToString() +
                                                    "' not supported yet!");
                            }
                    }
                }
            }
            // if aDoTest is true, we check ECX for exception flags
            if (!aDoTest)
            {
                //new CPU.Call("_CODE_REQUESTED_BREAK_");
                if (xJumpTo == null)
                {
                    Jump_Exception(aMethodInfo);
                }
                else
                {
                    XS.Jump(xJumpTo);
                }

            }
            else
            {
                XS.Test(ECX, 2);

                if (aCleanup != null)
                {
                    XS.Jump(CPU.ConditionalTestEnum.Equal, aJumpTargetNoException);
                    aCleanup();
                    if (xJumpTo == null)
                    {
                        XS.Jump(CPU.ConditionalTestEnum.NotEqual,GetLabel(aMethodInfo) + AppAssembler.EndOfMethodLabelNameException);
                    }
                    else
                    {
                        XS.Jump(CPU.ConditionalTestEnum.NotEqual, xJumpTo);
                    }
                }
                else
                {
                    if (xJumpTo == null)
                    {
                        XS.Jump(CPU.ConditionalTestEnum.NotEqual, GetLabel(aMethodInfo) + AppAssembler.EndOfMethodLabelNameException);
                    }
                    else
                    {
                        XS.Jump(CPU.ConditionalTestEnum.NotEqual, xJumpTo);
                    }
                }
            }
        }


        protected static void DoNullReferenceCheck(Assembler assembler, bool debugEnabled, int stackOffsetToCheck)
        {
            if (stackOffsetToCheck != SignedAlign(stackOffsetToCheck, 4))
            {
                throw new Exception("Stack offset not aligned!");
            }
            if (debugEnabled)
            {
                //if (!CompilerEngine.UseGen3Kernel) {
                XS.Compare(ESP, 0, destinationDisplacement: stackOffsetToCheck);
                XS.Jump(CPU.ConditionalTestEnum.NotEqual, ".AfterNullCheck");
                XS.ClearInterruptFlag();
                XS.Exchange(BX, BX);
                // don't remove the call. It seems pointless, but we need it to retrieve the EIP value
                XS.Call(".NullCheck_GetCurrAddress");
                XS.Label(".NullCheck_GetCurrAddress");
                XS.Pop(EAX);
                XS.Set(AsmMarker.Labels[AsmMarker.Type.DebugStub_CallerEIP], EAX, destinationIsIndirect: true);
                XS.Call(AsmMarker.Labels[AsmMarker.Type.DebugStub_SendNullRefEvent]);
                //}
                XS.Halt();
                XS.Label(".AfterNullCheck");
            }
        }

        public static _FieldInfo ResolveField(Type aDeclaringType, string aField, bool aOnlyInstance)
        {
            var xFields = GetFieldsInfo(aDeclaringType, !aOnlyInstance);
            var xFieldInfo = (from item in xFields
                              where item.Id == aField
                                    && (!aOnlyInstance || item.IsStatic == false)
                              select item).SingleOrDefault();
            if (xFieldInfo == null)
            {
                Console.WriteLine("Following fields have been found on '{0}'", aDeclaringType.FullName);
                foreach (var xField in xFields)
                {
                    Console.WriteLine("\t'{0}'", xField.Id);
                }
                throw new Exception(String.Format("Field '{0}' not found on type '{1}'", aField, aDeclaringType.FullName));
            }
            return xFieldInfo;
        }

        public static _FieldInfo ResolveField(FieldInfo fieldInfo)
        {
            var fieldsInfo = GetFieldsInfo(fieldInfo.DeclaringType, fieldInfo.IsStatic);
            return fieldsInfo.SingleOrDefault(
                f => MemberInfoComparer.Instance.Equals(f.Field, fieldInfo))
                ?? ResolveField(fieldInfo.DeclaringType, fieldInfo.GetFullName(), !fieldInfo.IsStatic);
        }

        protected static void CopyValue(Register32 destination, int destinationDisplacement, Register32 source, int sourceDisplacement, uint size)
        {
            for (int i = 0; i < (size / 4); i++)
            {
                XS.Set(EAX, source, sourceDisplacement: sourceDisplacement + (i * 4));
                XS.Set(destination, EAX, destinationDisplacement: destinationDisplacement + (i * 4));
            }
            switch (size % 4)
            {
                case 1:
                    XS.Set(AL, source, sourceDisplacement: (int)(sourceDisplacement + ((size / 4) * 4)));
                    XS.Set(destination, AL,
                      destinationDisplacement: (int)(destinationDisplacement + ((size / 4) * 4)));
                    break;
                case 2:
                    XS.Set(AX, source, sourceDisplacement: (int)(sourceDisplacement + ((size / 4) * 4)));
                    XS.Set(destination, AX,
                      destinationDisplacement: (int)(destinationDisplacement + ((size / 4) * 4)));
                    break;
                case 0:
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public static bool IsReferenceType(Type aType)
        {
            return !aType.IsValueType && !aType.IsPointer && !aType.IsByRef;
        }

        public static bool TypeIsSigned(Type aType)
        {
            var name = aType.FullName;
            //return "System.Char" == name || "System.SByte" == name || "System.Int16" == name ||
            //  "System.Int32" == name || "System.Int64" == name;

            return "System.SByte" == name || "System.Int16" == name ||
                        "System.Int32" == name || "System.Int64" == name;
        }

        /// <summary>
        /// Check if the type is represented as a int by the CLI
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsIntegerBasedType(Type type)
        {
            return type == BaseTypes.Byte || type == BaseTypes.Boolean || type == BaseTypes.SByte || type == BaseTypes.UInt16 || type == BaseTypes.Int16
                   || type == BaseTypes.Int32 || type == BaseTypes.UInt32
                   || type == BaseTypes.Char || type == BaseTypes.IntPtr || type == BaseTypes.UIntPtr;
        }

        public static bool IsLongBasedType(Type type)
        {
            return type == BaseTypes.Int64 || type == BaseTypes.UInt64;
        }

        public static bool IsSameValueType(Type aType, Type bType)
        {
            return (IsIntegerBasedType(aType) && IsIntegerBasedType(bType)) || (IsLongBasedType(aType) && IsLongBasedType(bType))
                || (IsPointer(aType) && IsPointer(bType) || (aType == bType && (aType == BaseTypes.Double || aType == BaseTypes.Single)));
        }

        /// <summary>
        /// Is the type a numeric type or a pointer
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsIntegralTypeOrPointer(Type type)
        {
            return IsIntegerBasedType(type) || IsLongBasedType(type) || type.IsPointer || type.IsByRef;
        }

        public static bool IsPointer(Type aPointer)
        {
            return aPointer.IsPointer || aPointer.IsByRef || aPointer == BaseTypes.IntPtr || aPointer == BaseTypes.UIntPtr;
        }

        public static bool IsObject(Type aPointer)
        {
            return aPointer.IsMyAssignableTo(BaseTypes.Object) || aPointer == Base.NullRef;
        }

        public static bool IsByRef(Type aType) => aType.IsByRef;

        public static uint SizeOfType(Type aType)
        {
            if (aType == null)
            {
                throw new ArgumentNullException(nameof(aType));
            }
            if (aType.IsEnum)
            {
                aType.GetEnumUnderlyingType();
            }
            if (aType.IsPointer || aType.IsByRef)
            {
                return 4;
            }
            if (aType.FullName == "System.Void")
            {
                return 0;
            }
            if (IsReferenceType(aType))
            {
                return 8;
            }
#pragma warning disable IDE0010 // Add missing cases
            switch (aType.FullName)
#pragma warning restore IDE0010 // Add missing cases
            {
                case "System.Char":
                    return 2;
                case "System.Byte":
                case "System.SByte":
                    return 1;
                case "System.UInt16":
                case "System.Int16":
                    return 2;
                case "System.UInt32":
                case "System.Int32":
                    return 4;
                case "System.UInt64":
                case "System.Int64":
                    return 8;
                //TODO: for now hardcode IntPtr and UIntPtr to be 32-bit
                case "System.UIntPtr":
                case "System.IntPtr":
                    return 4;
                case "System.Boolean":
                    return 1;
                case "System.Single":
                    return 4;
                case "System.Double":
                    return 8;
                case "System.Decimal":
                    return 16;
                case "System.Guid":
                    return 16;
                case "System.DateTime":
                    return 8;
            }
            if (aType.FullName != null && aType.FullName.EndsWith("*"))
            {
                // pointer
                return 4;
            }
            // array
            //TypeSpecification xTypeSpec = aType as TypeSpecification;
            //if (xTypeSpec != null) {
            //    return 4;
            //}
            if (aType.IsEnum)
            {
                return SizeOfType(aType.GetField("value__").FieldType);
            }
            if (aType.IsValueType && aType != Base.ValueType)
            {
                // structs are stored in the stack, so stack size = storage size
                return GetStorageSize(aType);
            }
            return 4;
        }

        protected static bool TypeIsFloat(Type type)
        {
            return type == BaseTypes.Single || type == BaseTypes.Double;
        }
    }
}
