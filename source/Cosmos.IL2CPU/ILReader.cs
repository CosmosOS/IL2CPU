using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;

using Cosmos.IL2CPU.Extensions;
using IL2CPU.Reflection;

using static IL2CPU.Reflection.BaseTypeSystem;

namespace Cosmos.IL2CPU
{
    public class ILReader
    {
        // We split this into two arrays since we have to read
        // a byte at a time anways. In the future if we need to
        // back to a unifed array, instead of 64k entries
        // we can change it to a signed int, and then add x0200 to the value.
        // This will reduce array size down to 768 entries.
        private static readonly OpCode[] mOpCodesLo = new OpCode[256];
        private static readonly OpCode[] mOpCodesHi = new OpCode[256];

        public ILReader()
        {
            LoadOpCodes();
        }

        protected void LoadOpCodes()
        {
            foreach (var xField in typeof(OpCodes).GetFields(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public))
            {
                var xOpCode = (OpCode)xField.GetValue(null);
                var xValue = (ushort)xOpCode.Value;
                if (xValue <= 0xFF)
                {
                    mOpCodesLo[xValue] = xOpCode;
                }
                else
                {
                    mOpCodesHi[xValue & 0xFF] = xOpCode;
                }
            }
        }

        protected void CheckBranch(int aTarget, int aMethodSize)
        {
            // this method is a safety-measure. Should never occur
            if (aTarget < 0 || aTarget >= aMethodSize)
            {
                throw new Exception("Branch jumps outside method.");
            }
        }

        public List<ILOpCode> ProcessMethod(MethodBase aMethod)
        {
            var xResult = new List<ILOpCode>();

            var xBody = aMethod.GetMethodBody();
            var xModule = aMethod.Module;

            // Cache for use in field and method resolution
            Type[] xTypeGenArgs = Type.EmptyTypes;
            Type[] xMethodGenArgs = Type.EmptyTypes;
            if (aMethod.DeclaringType.IsGenericType)
            {
                xTypeGenArgs = aMethod.DeclaringType.GetGenericArguments();
            }
            if (aMethod.IsGenericMethod)
            {
                xMethodGenArgs = aMethod.GetGenericArguments();
            }

            #region Unsafe Intrinsic

            if (aMethod.DeclaringType.FullName == "Internal.Runtime.CompilerServices.Unsafe")
            {
                var xUnsafeType = Type.GetType("System.Runtime.CompilerServices.Unsafe, System.Runtime.CompilerServices.Unsafe");
                var xUnsafeMethod = xUnsafeType.GetMethods()
                    .Where(
                        m => m.Name == aMethod.Name
                        && m.GetGenericArguments().Length == aMethod.GetGenericArguments().Length
                        && m.GetParameters().Length == aMethod.GetParameters().Length)
                    .SingleOrDefault(
                        m =>
                        {
                            var xParamTypes = Array.ConvertAll(m.GetParameters(), p => p.ParameterType);
                            var xOriginalParamTypes = Array.ConvertAll(
                                ((MethodInfo)aMethod).GetParameters(), p => p.ParameterType);

                            for (int i = 0; i < xParamTypes.Length; i++)
                            {
                                var xParamType = xParamTypes[i];
                                var xOriginalParamType = xOriginalParamTypes[i];

                                while (xParamType.HasElementType)
                                {
                                    if (!xOriginalParamType.HasElementType)
                                    {
                                        return false;
                                    }

                                    if ((xParamType.IsArray && !xOriginalParamType.IsArray)
                                        || (xParamType.IsByRef && !xOriginalParamType.IsByRef)
                                        || (xParamType.IsPointer && !xOriginalParamType.IsPointer))
                                    {
                                        return false;
                                    }

                                    xParamType = xParamType.GetElementType();
                                    xOriginalParamType = xOriginalParamType.GetElementType();
                                }

                                if (!xParamType.IsAssignableFrom(xOriginalParamType)
                                    && (!xParamType.IsGenericParameter || (xParamType.HasElementType && !xParamType.IsArray)))
                                {
                                    return false;
                                }
                            }

                            return true;
                        });

                if (xUnsafeMethod != null)
                {
                    xBody = xUnsafeMethod.GetMethodBody();
                    xModule = xUnsafeMethod.Module;
                }
            }

            #endregion


            #region ByReference Intrinsic

            if (aMethod.DeclaringType.IsGenericType
                && aMethod.DeclaringType.GetGenericTypeDefinition().FullName == "System.ByReference`1")
            {
                var valueField = aMethod.DeclaringType.GetField("_value", BindingFlags.Instance | BindingFlags.NonPublic);

                switch (aMethod.Name)
                {
                    case ".ctor":

                        // push $this
                        xResult.Add(new ILOpCodes.OpVar(ILOpCode.Code.Ldarg, 0, 1, 0, null));

                        // push value (arg 1)
                        xResult.Add(new ILOpCodes.OpVar(ILOpCode.Code.Ldarg, 1, 2, 1, null));

                        // store value into $this._value
                        xResult.Add(new ILOpCodes.OpField(ILOpCode.Code.Stfld, 2, 8, valueField, null));

                        // return
                        xResult.Add(new ILOpCodes.OpNone(ILOpCode.Code.Ret, 8, 9, null));

                        break;

                    case "get_Value":

                        // push $this
                        xResult.Add(new ILOpCodes.OpVar(ILOpCode.Code.Ldarg, 0, 1, 0, null));

                        // push $this._value
                        xResult.Add(new ILOpCodes.OpField(ILOpCode.Code.Ldfld, 1, 6, valueField, null));

                        // return
                        xResult.Add(new ILOpCodes.OpNone(ILOpCode.Code.Ret, 6, 7, null));

                        break;

                    default:
                        throw new NotImplementedException($"ByReference intrinsic method '{aMethod}' not implemented!");
                }

                foreach (var op in xResult)
                {
                    op.InitStackAnalysis(aMethod);
                }

                return xResult;
            }

            #endregion

            #region RuntimeTypeHandle

            if(aMethod.DeclaringType.Name == "RuntimeType")
            {
                if(aMethod.Name == ".ctor")
                {
                    var op = new ILOpCodes.OpNone(ILOpCode.Code.Ret, 0, 1, null);
                    op.InitStackAnalysis(aMethod);

                    xResult.Add(op);

                    return xResult;
                }
            }

            if (aMethod.DeclaringType.Name == "TypeImpl")
            {
                if (aMethod.Name == "CreateRuntimeTypeHandle")
                {
                    // the idea of this method is to first create a RuntimeType object, set its handle and then create a RuntimeTypeHandle from it
                    // we are manually coding in il here since we have to call a internal method on an internal class
                    var runtimeType = Type.GetType("System.RuntimeType");
                    var ctor = runtimeType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] {  }, null);
                    xResult.Add(new ILOpCodes.OpMethod(ILOpCode.Code.Newobj, 0, 1, ctor, null) {
                        StackPopTypes = Array.Empty<Type>(),
                        StackPushTypes = new[] { runtimeType },
                    });
                    xResult.Add(new ILOpCodes.OpNone(ILOpCode.Code.Dup, 1, 2, null)
                    {
                        StackPopTypes = new[] { runtimeType },
                        StackPushTypes = new[] { runtimeType, runtimeType }
                    });
                    xResult.Add(new ILOpCodes.OpVar(ILOpCode.Code.Ldarg, 2, 3, 0, null) {
                        StackPopTypes = Array.Empty<Type>(),
                        StackPushTypes = new[] { BaseTypes.Int32 },
                    });
                    var m_handle = runtimeType.GetField("m_handle", BindingFlags.Instance | BindingFlags.NonPublic);
                    xResult.Add(new ILOpCodes.OpField(ILOpCode.Code.Stfld, 3, 4, m_handle, null) {
                        StackPopTypes = new[] {BaseTypes.Int32, runtimeType},
                        StackPushTypes = Array.Empty<Type>(),
                    });
                    var runtimeTypeHandle = Type.GetType("System.RuntimeTypeHandle");
                    ctor = runtimeTypeHandle.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { runtimeType }, null);
                    xResult.Add(new ILOpCodes.OpMethod(ILOpCode.Code.Newobj, 4, 5, ctor, null)
                    {
                        StackPopTypes = new[] { runtimeType },
                        StackPushTypes = new[] { runtimeTypeHandle },
                    });
                    xResult.Add(new ILOpCodes.OpNone(ILOpCode.Code.Ret, 5, 6, null)
                    {
                        StackPopTypes = Array.Empty<Type>(),
                        StackPushTypes = Array.Empty<Type>(),
                    });

                    return xResult;
                }   
            }
            #endregion

            #region ArrayPool ("hacked" generic plug)

            if (aMethod.DeclaringType.IsGenericType
                && aMethod.DeclaringType.GetGenericTypeDefinition().FullName == "System.Buffers.ArrayPool`1")
            {
                if (aMethod.Name == ".cctor")
                {
                    var op = new ILOpCodes.OpNone(ILOpCode.Code.Ret, 0, 1, null);
                    op.InitStackAnalysis(aMethod);

                    xResult.Add(op);

                    return xResult;
                }
            }

            #endregion

            #region RuntimeHelpers

            if (aMethod.DeclaringType.FullName == "System.Runtime.CompilerServices.RuntimeHelpers")
            {
                if (aMethod.Name == "IsBitwiseEquatable")
                {
                    // This is a generic method so we emit true or false depending on the type
                    ILOpCode op;
                    if (ILOp.IsIntegralTypeOrPointer(xMethodGenArgs[0]))
                    {
                        op = new ILOpCodes.OpInt(ILOpCode.Code.Ldc_I4, 0, 1, 1, null);
                    }
                    else
                    {
                        op = new ILOpCodes.OpInt(ILOpCode.Code.Ldc_I4, 0, 1, 1, null);
                    }
                    op.InitStackAnalysis(aMethod);
                    xResult.Add(op);

                    op = new ILOpCodes.OpNone(ILOpCode.Code.Ret, 1, 2, null);
                    op.InitStackAnalysis(aMethod);

                    xResult.Add(op);

                    return xResult;
                }
            }

            #endregion

            // Some methods return no body. Not sure why.. have to investigate
            // They arent abstracts or icalls...
            if (xBody == null)
            {
                return null;
            }

            var xIL = xBody.GetILAsByteArray();
            int xPos = 0;
            while (xPos < xIL.Length)
            {
                _ExceptionRegionInfo xCurrentExceptionRegion = null;
                #region Determine current handler
                // todo: add support for nested handlers using a stack or so..
                foreach (_ExceptionRegionInfo xHandler in aMethod.GetExceptionRegionInfos())
                {
                    if (xHandler.TryOffset >= 0)
                    {
                        if (xHandler.TryOffset <= xPos && (xHandler.TryLength + xHandler.TryOffset) > xPos)
                        {
                            if (xCurrentExceptionRegion == null)
                            {
                                xCurrentExceptionRegion = xHandler;
                                continue;
                            }
                            else if (xHandler.TryOffset > xCurrentExceptionRegion.TryOffset && (xHandler.TryLength + xHandler.TryOffset) < (xCurrentExceptionRegion.TryLength + xCurrentExceptionRegion.TryOffset))
                            {
                                // only replace if the current found handler is narrower
                                xCurrentExceptionRegion = xHandler;
                                continue;
                            }
                        }
                    }
                    // todo: handler offset can be 0 like try offset?
                    if (xHandler.HandlerOffset > 0)
                    {
                        if (xHandler.HandlerOffset <= xPos && (xHandler.HandlerOffset + xHandler.HandlerLength) > xPos)
                        {
                            if (xCurrentExceptionRegion == null)
                            {
                                xCurrentExceptionRegion = xHandler;
                                continue;
                            }
                            else if (xHandler.HandlerOffset > xCurrentExceptionRegion.HandlerOffset && (xHandler.HandlerOffset + xHandler.HandlerLength) < (xCurrentExceptionRegion.HandlerOffset + xCurrentExceptionRegion.HandlerLength))
                            {
                                // only replace if the current found handler is narrower
                                xCurrentExceptionRegion = xHandler;
                                continue;
                            }
                        }
                    }
                    if (xHandler.Kind.HasFlag(ExceptionRegionKind.Filter))
                    {
                        if (xHandler.FilterOffset > 0)
                        {
                            if (xHandler.FilterOffset <= xPos)
                            {
                                if (xCurrentExceptionRegion == null)
                                {
                                    xCurrentExceptionRegion = xHandler;
                                    continue;
                                }
                                else if (xHandler.FilterOffset > xCurrentExceptionRegion.FilterOffset)
                                {
                                    // only replace if the current found handler is narrower
                                    xCurrentExceptionRegion = xHandler;
                                    continue;
                                }
                            }
                        }
                    }
                }
                #endregion
                ILOpCode.Code xOpCodeVal;
                OpCode xOpCode;
                int xOpPos = xPos;
                if (xIL[xPos] == 0xFE)
                {
                    xOpCodeVal = (ILOpCode.Code)(0xFE00 | xIL[xPos + 1]);
                    xOpCode = mOpCodesHi[xIL[xPos + 1]];
                    xPos = xPos + 2;
                }
                else
                {
                    xOpCodeVal = (ILOpCode.Code)xIL[xPos];
                    xOpCode = mOpCodesLo[xIL[xPos]];
                    xPos++;
                }

                ILOpCode xILOpCode = null;
                switch (xOpCode.OperandType)
                {
                    // No operand.
                    case OperandType.InlineNone:
                        {
                            #region Inline none options
                            // These shortcut translation regions expand shortcut ops into full ops
                            // This elminates the amount of code required in the assemblers
                            // by allowing them to ignore the shortcuts
                            switch (xOpCodeVal)
                            {
                                case ILOpCode.Code.Ldarg_0:
                                    xILOpCode = new ILOpCodes.OpVar(ILOpCode.Code.Ldarg, xOpPos, xPos, 0, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Ldarg_1:
                                    xILOpCode = new ILOpCodes.OpVar(ILOpCode.Code.Ldarg, xOpPos, xPos, 1, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Ldarg_2:
                                    xILOpCode = new ILOpCodes.OpVar(ILOpCode.Code.Ldarg, xOpPos, xPos, 2, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Ldarg_3:
                                    xILOpCode = new ILOpCodes.OpVar(ILOpCode.Code.Ldarg, xOpPos, xPos, 3, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Ldc_I4_0:
                                    xILOpCode = new ILOpCodes.OpInt(ILOpCode.Code.Ldc_I4, xOpPos, xPos, 0, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Ldc_I4_1:
                                    xILOpCode = new ILOpCodes.OpInt(ILOpCode.Code.Ldc_I4, xOpPos, xPos, 1, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Ldc_I4_2:
                                    xILOpCode = new ILOpCodes.OpInt(ILOpCode.Code.Ldc_I4, xOpPos, xPos, 2, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Ldc_I4_3:
                                    xILOpCode = new ILOpCodes.OpInt(ILOpCode.Code.Ldc_I4, xOpPos, xPos, 3, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Ldc_I4_4:
                                    xILOpCode = new ILOpCodes.OpInt(ILOpCode.Code.Ldc_I4, xOpPos, xPos, 4, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Ldc_I4_5:
                                    xILOpCode = new ILOpCodes.OpInt(ILOpCode.Code.Ldc_I4, xOpPos, xPos, 5, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Ldc_I4_6:
                                    xILOpCode = new ILOpCodes.OpInt(ILOpCode.Code.Ldc_I4, xOpPos, xPos, 6, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Ldc_I4_7:
                                    xILOpCode = new ILOpCodes.OpInt(ILOpCode.Code.Ldc_I4, xOpPos, xPos, 7, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Ldc_I4_8:
                                    xILOpCode = new ILOpCodes.OpInt(ILOpCode.Code.Ldc_I4, xOpPos, xPos, 8, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Ldc_I4_M1:
                                    xILOpCode = new ILOpCodes.OpInt(ILOpCode.Code.Ldc_I4, xOpPos, xPos, -1, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Ldloc_0:
                                    xILOpCode = new ILOpCodes.OpVar(ILOpCode.Code.Ldloc, xOpPos, xPos, 0, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Ldloc_1:
                                    xILOpCode = new ILOpCodes.OpVar(ILOpCode.Code.Ldloc, xOpPos, xPos, 1, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Ldloc_2:
                                    xILOpCode = new ILOpCodes.OpVar(ILOpCode.Code.Ldloc, xOpPos, xPos, 2, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Ldloc_3:
                                    xILOpCode = new ILOpCodes.OpVar(ILOpCode.Code.Ldloc, xOpPos, xPos, 3, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Stloc_0:
                                    xILOpCode = new ILOpCodes.OpVar(ILOpCode.Code.Stloc, xOpPos, xPos, 0, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Stloc_1:
                                    xILOpCode = new ILOpCodes.OpVar(ILOpCode.Code.Stloc, xOpPos, xPos, 1, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Stloc_2:
                                    xILOpCode = new ILOpCodes.OpVar(ILOpCode.Code.Stloc, xOpPos, xPos, 2, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Stloc_3:
                                    xILOpCode = new ILOpCodes.OpVar(ILOpCode.Code.Stloc, xOpPos, xPos, 3, xCurrentExceptionRegion);
                                    break;
                                default:
                                    xILOpCode = new ILOpCodes.OpNone(xOpCodeVal, xOpPos, xPos, xCurrentExceptionRegion);
                                    break;
                            }
                            #endregion
                            break;
                        }

                    case OperandType.ShortInlineBrTarget:
                        {
                            #region Inline branch
                            // By calculating target, we assume all branches are within a method
                            // So far at least wtih csc, its true. We check it with CheckBranch
                            // just in case.
                            int xTarget = xPos + 1 + (sbyte)xIL[xPos];
                            CheckBranch(xTarget, xIL.Length);
                            switch (xOpCodeVal)
                            {
                                case ILOpCode.Code.Beq_S:
                                    xILOpCode = new ILOpCodes.OpBranch(ILOpCode.Code.Beq, xOpPos, xPos + 1, xTarget, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Bge_S:
                                    xILOpCode = new ILOpCodes.OpBranch(ILOpCode.Code.Bge, xOpPos, xPos + 1, xTarget, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Bge_Un_S:
                                    xILOpCode = new ILOpCodes.OpBranch(ILOpCode.Code.Bge_Un, xOpPos, xPos + 1, xTarget, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Bgt_S:
                                    xILOpCode = new ILOpCodes.OpBranch(ILOpCode.Code.Bgt, xOpPos, xPos + 1, xTarget, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Bgt_Un_S:
                                    xILOpCode = new ILOpCodes.OpBranch(ILOpCode.Code.Bgt_Un, xOpPos, xPos + 1, xTarget, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Ble_S:
                                    xILOpCode = new ILOpCodes.OpBranch(ILOpCode.Code.Ble, xOpPos, xPos + 1, xTarget, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Ble_Un_S:
                                    xILOpCode = new ILOpCodes.OpBranch(ILOpCode.Code.Ble_Un, xOpPos, xPos + 1, xTarget, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Blt_S:
                                    xILOpCode = new ILOpCodes.OpBranch(ILOpCode.Code.Blt, xOpPos, xPos + 1, xTarget, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Blt_Un_S:
                                    xILOpCode = new ILOpCodes.OpBranch(ILOpCode.Code.Blt_Un, xOpPos, xPos + 1, xTarget, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Bne_Un_S:
                                    xILOpCode = new ILOpCodes.OpBranch(ILOpCode.Code.Bne_Un, xOpPos, xPos + 1, xTarget, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Br_S:
                                    xILOpCode = new ILOpCodes.OpBranch(ILOpCode.Code.Br, xOpPos, xPos + 1, xTarget, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Brfalse_S:
                                    xILOpCode = new ILOpCodes.OpBranch(ILOpCode.Code.Brfalse, xOpPos, xPos + 1, xTarget, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Brtrue_S:
                                    xILOpCode = new ILOpCodes.OpBranch(ILOpCode.Code.Brtrue, xOpPos, xPos + 1, xTarget, xCurrentExceptionRegion);
                                    break;
                                case ILOpCode.Code.Leave_S:
                                    xILOpCode = new ILOpCodes.OpBranch(ILOpCode.Code.Leave, xOpPos, xPos + 1, xTarget, xCurrentExceptionRegion);
                                    break;
                                default:
                                    xILOpCode = new ILOpCodes.OpBranch(xOpCodeVal, xOpPos, xPos + 1, xTarget, xCurrentExceptionRegion);
                                    break;
                            }
                            xPos = xPos + 1;
                            break;
                            #endregion
                        }
                    case OperandType.InlineBrTarget:
                        {
                            int xTarget = xPos + 4 + ReadInt32(xIL, xPos);
                            CheckBranch(xTarget, xIL.Length);
                            xILOpCode = new ILOpCodes.OpBranch(xOpCodeVal, xOpPos, xPos + 4, xTarget, xCurrentExceptionRegion);
                            xPos = xPos + 4;
                            break;
                        }

                    case OperandType.ShortInlineI:
                        switch (xOpCodeVal)
                        {
                            case ILOpCode.Code.Ldc_I4_S:
                                xILOpCode = new ILOpCodes.OpInt(ILOpCode.Code.Ldc_I4, xOpPos, xPos + 1, ((sbyte)xIL[xPos]), xCurrentExceptionRegion);
                                break;
                            default:
                                xILOpCode = new ILOpCodes.OpInt(xOpCodeVal, xOpPos, xPos + 1, ((sbyte)xIL[xPos]), xCurrentExceptionRegion);
                                break;
                        }
                        xPos = xPos + 1;
                        break;
                    case OperandType.InlineI:
                        xILOpCode = new ILOpCodes.OpInt(xOpCodeVal, xOpPos, xPos + 4, ReadInt32(xIL, xPos), xCurrentExceptionRegion);
                        xPos = xPos + 4;
                        break;
                    case OperandType.InlineI8:
                        xILOpCode = new ILOpCodes.OpInt64(xOpCodeVal, xOpPos, xPos + 8, ReadUInt64(xIL, xPos), xCurrentExceptionRegion);
                        xPos = xPos + 8;
                        break;

                    case OperandType.ShortInlineR:
                        xILOpCode = new ILOpCodes.OpSingle(xOpCodeVal, xOpPos, xPos + 4, BitConverter.ToSingle(xIL, xPos), xCurrentExceptionRegion);
                        xPos = xPos + 4;
                        break;
                    case OperandType.InlineR:
                        xILOpCode = new ILOpCodes.OpDouble(xOpCodeVal, xOpPos, xPos + 8, BitConverter.ToDouble(xIL, xPos), xCurrentExceptionRegion);
                        xPos = xPos + 8;
                        break;

                    // The operand is a 32-bit metadata token.
                    case OperandType.InlineField:
                        {
                            var xValue = xModule.ResolveMyField(ReadInt32(xIL, xPos), xTypeGenArgs, xMethodGenArgs);
                            xILOpCode = new ILOpCodes.OpField(xOpCodeVal, xOpPos, xPos + 4, xValue, xCurrentExceptionRegion);
                            xPos = xPos + 4;
                            break;
                        }

                    // The operand is a 32-bit metadata token.
                    case OperandType.InlineMethod:
                        {
                            var xValue = xModule.ResolveMyMethod(ReadInt32(xIL, xPos), xTypeGenArgs, xMethodGenArgs);
                            xILOpCode = new ILOpCodes.OpMethod(xOpCodeVal, xOpPos, xPos + 4, xValue, xCurrentExceptionRegion);
                            xPos = xPos + 4;
                            break;
                        }

                    // 32-bit metadata signature token.
                    case OperandType.InlineSig:
                        xILOpCode = new ILOpCodes.OpSig(xOpCodeVal, xOpPos, xPos + 4, ReadInt32(xIL, xPos), xCurrentExceptionRegion);
                        xPos = xPos + 4;
                        break;

                    case OperandType.InlineString:
                        xILOpCode = new ILOpCodes.OpString(xOpCodeVal, xOpPos, xPos + 4, xModule.ResolveMyString(ReadInt32(xIL, xPos)), xCurrentExceptionRegion);
                        xPos = xPos + 4;
                        break;

                    case OperandType.InlineSwitch:
                        {
                            int xCount = ReadInt32(xIL, xPos);
                            xPos = xPos + 4;
                            int xNextOpPos = xPos + xCount * 4;
                            var xBranchLocations = new int[xCount];
                            for (int i = 0; i < xCount; i++)
                            {
                                xBranchLocations[i] = xNextOpPos + ReadInt32(xIL, xPos + i * 4);
                                CheckBranch(xBranchLocations[i], xIL.Length);
                            }
                            xILOpCode = new ILOpCodes.OpSwitch(xOpCodeVal, xOpPos, xNextOpPos, xBranchLocations, xCurrentExceptionRegion);
                            xPos = xNextOpPos;
                            break;
                        }

                    // The operand is a FieldRef, MethodRef, or TypeRef token.
                    case OperandType.InlineTok:
                        xILOpCode = new ILOpCodes.OpToken(xOpCodeVal, xOpPos, xPos + 4, ReadInt32(xIL, xPos), xModule, xTypeGenArgs, xMethodGenArgs, xCurrentExceptionRegion);
                        xPos = xPos + 4;
                        break;

                    // 32-bit metadata token.
                    case OperandType.InlineType:
                        {
                            var xValue = xModule.ResolveMyType(ReadInt32(xIL, xPos), xTypeGenArgs, xMethodGenArgs);
                            xILOpCode = new ILOpCodes.OpType(xOpCodeVal, xOpPos, xPos + 4, xValue, xCurrentExceptionRegion);
                            xPos = xPos + 4;
                            break;
                        }

                    case OperandType.ShortInlineVar:
                        switch (xOpCodeVal)
                        {
                            case ILOpCode.Code.Ldloc_S:
                                xILOpCode = new ILOpCodes.OpVar(ILOpCode.Code.Ldloc, xOpPos, xPos + 1, xIL[xPos], xCurrentExceptionRegion);
                                break;
                            case ILOpCode.Code.Ldloca_S:
                                xILOpCode = new ILOpCodes.OpVar(ILOpCode.Code.Ldloca, xOpPos, xPos + 1, xIL[xPos], xCurrentExceptionRegion);
                                break;
                            case ILOpCode.Code.Ldarg_S:
                                xILOpCode = new ILOpCodes.OpVar(ILOpCode.Code.Ldarg, xOpPos, xPos + 1, xIL[xPos], xCurrentExceptionRegion);
                                break;
                            case ILOpCode.Code.Ldarga_S:
                                xILOpCode = new ILOpCodes.OpVar(ILOpCode.Code.Ldarga, xOpPos, xPos + 1, xIL[xPos], xCurrentExceptionRegion);
                                break;
                            case ILOpCode.Code.Starg_S:
                                xILOpCode = new ILOpCodes.OpVar(ILOpCode.Code.Starg, xOpPos, xPos + 1, xIL[xPos], xCurrentExceptionRegion);
                                break;
                            case ILOpCode.Code.Stloc_S:
                                xILOpCode = new ILOpCodes.OpVar(ILOpCode.Code.Stloc, xOpPos, xPos + 1, xIL[xPos], xCurrentExceptionRegion);
                                break;
                            default:
                                xILOpCode = new ILOpCodes.OpVar(xOpCodeVal, xOpPos, xPos + 1, xIL[xPos], xCurrentExceptionRegion);
                                break;
                        }
                        xPos = xPos + 1;
                        break;
                    case OperandType.InlineVar:
                        xILOpCode = new ILOpCodes.OpVar(xOpCodeVal, xOpPos, xPos + 2, ReadUInt16(xIL, xPos), xCurrentExceptionRegion);
                        xPos = xPos + 2;
                        break;

                    default:
                        throw new Exception("Unknown OperandType");
                }
                xILOpCode.InitStackAnalysis(aMethod);
                xResult.Add(xILOpCode);
            }

            return xResult;
        }

        // We could use BitConvertor, unfortuantely they "hardcoded" endianness. Its fine for reading IL now...
        // but they essentially do the same as we do, just a bit slower.
        private ushort ReadUInt16(byte[] aBytes, int aPos)
        {
            return (ushort)(aBytes[aPos + 1] << 8 | aBytes[aPos]);
        }

        private int ReadInt32(byte[] aBytes, int aPos)
        {
            return aBytes[aPos + 3] << 24 | aBytes[aPos + 2] << 16 | aBytes[aPos + 1] << 8 | aBytes[aPos];
        }

        private ulong ReadUInt64(byte[] aBytes, int aPos)
        {
            //return (UInt64)(
            //  aBytes[aPos + 7] << 56 | aBytes[aPos + 6] << 48 | aBytes[aPos + 5] << 40 | aBytes[aPos + 4] << 32
            //  | aBytes[aPos + 3] << 24 | aBytes[aPos + 2] << 16 | aBytes[aPos + 1] << 8 | aBytes[aPos]);

            return BitConverter.ToUInt64(aBytes, aPos);
        }

    }
}
