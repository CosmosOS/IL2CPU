using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using OpCode = System.Reflection.Metadata.ILOpCode;

using Cosmos.IL2CPU.ILOpCodes;
using IL2CPU.Reflection;

namespace Cosmos.IL2CPU
{
    public class ILVisitor : IILVisitor
    {
        public IReadOnlyList<ILOpCode> ILOpCodes => _ilOpCodes;

        private readonly MethodInfo _method;

        private readonly List<ILOpCode> _ilOpCodes = new List<ILOpCode>();

        public ILVisitor(MethodInfo method)
        {
            _method = method;
        }

        public void OnInlineBrTarget(OpCode opCode, int pos, int nextPos, int target) =>
            AddILOpCode(new OpBranch((ILOpCode.Code)opCode, pos, nextPos, target, FindExceptionBlock(pos)));

        public void OnInlineField(OpCode opCode, int pos, int nextPos, FieldInfo field) =>
            AddILOpCode(new OpField((ILOpCode.Code)opCode, pos, nextPos, field, FindExceptionBlock(pos)));

        public void OnInlineI(OpCode opCode, int pos, int nextPos, int value) =>
            AddILOpCode(new OpInt((ILOpCode.Code)opCode, pos, nextPos, value, FindExceptionBlock(pos)));

        public void OnInlineI8(OpCode opCode, int pos, int nextPos, long value) =>
            AddILOpCode(new OpInt64((ILOpCode.Code)opCode, pos, nextPos, value, FindExceptionBlock(pos)));

        public void OnInlineMethod(OpCode opCode, int pos, int nextPos, MethodInfo method) =>
            AddILOpCode(new OpMethod((ILOpCode.Code)opCode, pos, nextPos, method, FindExceptionBlock(pos)));

        public void OnInlineNone(OpCode opCode, int pos, int nextPos)
        {
            var exceptionBlock = FindExceptionBlock(pos);

            switch (opCode)
            {
                case OpCode.Ldarg_0:
                case OpCode.Ldarg_1:
                case OpCode.Ldarg_2:
                case OpCode.Ldarg_3:
                    AddILOpCode(new OpVar(ILOpCode.Code.Ldarg, pos, nextPos, opCode - OpCode.Ldarg_0, exceptionBlock));
                    return;
                case OpCode.Ldc_i4_0:
                case OpCode.Ldc_i4_1:
                case OpCode.Ldc_i4_2:
                case OpCode.Ldc_i4_3:
                case OpCode.Ldc_i4_4:
                case OpCode.Ldc_i4_5:
                case OpCode.Ldc_i4_6:
                case OpCode.Ldc_i4_7:
                case OpCode.Ldc_i4_8:
                case OpCode.Ldc_i4_m1:
                    // the (short) cast is needed for the m1 case
                    AddILOpCode(new OpInt(ILOpCode.Code.Ldc_I4, pos, nextPos, (short)(opCode - OpCode.Ldc_i4_0), exceptionBlock));
                    return;
                case OpCode.Ldloc_0:
                case OpCode.Ldloc_1:
                case OpCode.Ldloc_2:
                case OpCode.Ldloc_3:
                    AddILOpCode(new OpVar(ILOpCode.Code.Ldloc, pos, nextPos, opCode - OpCode.Ldloc_0, exceptionBlock));
                    return;
                case OpCode.Stloc_0:
                case OpCode.Stloc_1:
                case OpCode.Stloc_2:
                case OpCode.Stloc_3:
                    AddILOpCode(new OpVar(ILOpCode.Code.Stloc, pos, nextPos, opCode - OpCode.Stloc_0, exceptionBlock));
                    return;
                default:
                    AddILOpCode(new OpNone((ILOpCode.Code)opCode, pos, nextPos, exceptionBlock));
                    return;
            }

        }

        public void OnInlineR(OpCode opCode, int pos, int nextPos, double value) =>
            AddILOpCode(new OpDouble((ILOpCode.Code)opCode, pos, nextPos, value, FindExceptionBlock(pos)));

        public void OnInlineSig(OpCode opCode, int pos, int nextPos, MethodSignature<TypeInfo> signature) =>
            AddILOpCode(new OpSig((ILOpCode.Code)opCode, pos, nextPos, signature, FindExceptionBlock(pos)));

        public void OnInlineString(OpCode opCode, int pos, int nextPos, string value) =>
            AddILOpCode(new OpString((ILOpCode.Code)opCode, pos, nextPos, value, FindExceptionBlock(pos)));

        public void OnInlineSwitch(OpCode opCode, int pos, int nextPos, ImmutableArray<int> branchLocations) =>
            AddILOpCode(new OpSwitch((ILOpCode.Code)opCode, pos, nextPos, branchLocations, FindExceptionBlock(pos)));

        public void OnInlineTok(OpCode opCode, int pos, int nextPos, int metadataToken,
            ModuleInfo module, IReadOnlyList<TypeInfo> typeArguments, IReadOnlyList<TypeInfo> methodArguments) =>
            AddILOpCode(new OpToken((ILOpCode.Code)opCode, pos, nextPos, metadataToken, module, typeArguments, methodArguments, FindExceptionBlock(pos)));

        public void OnInlineType(OpCode opCode, int pos, int nextPos, TypeInfo type) =>
            AddILOpCode(new OpType((ILOpCode.Code)opCode, pos, nextPos, type, FindExceptionBlock(pos)));

        public void OnInlineVar(OpCode opCode, int pos, int nextPos, ushort index) =>
            AddILOpCode(new OpVar((ILOpCode.Code)opCode, pos, nextPos, index, FindExceptionBlock(pos)));

        public void OnShortInlineBrTarget(OpCode opCode, int pos, int nextPos, int target) =>
            AddILOpCode(new OpBranch((ILOpCode.Code)(opCode.GetLongBranch()), pos, nextPos, target, FindExceptionBlock(pos)));

        public void OnShortInlineI(OpCode opCode, int pos, int nextPos, byte value)
        {
            var exceptionBlock = FindExceptionBlock(pos);

            switch (opCode)
            {
                case OpCode.Ldc_i4_s:
                    // the (sbyte) cast is needed because ldc.i4.s takes a signed integer.
                    AddILOpCode(new OpInt(ILOpCode.Code.Ldc_I4, pos, nextPos, (sbyte)value, exceptionBlock));
                    return;
                default:
                    AddILOpCode(new OpInt((ILOpCode.Code)opCode, pos, nextPos, value, exceptionBlock));
                    return;
            }
        }

        public void OnShortInlineR(OpCode opCode, int pos, int nextPos, float value) =>
            AddILOpCode(new OpSingle((ILOpCode.Code)opCode, pos, nextPos, value, FindExceptionBlock(pos)));

        public void OnShortInlineVar(OpCode opCode, int pos, int nextPos, ushort index) =>
            AddILOpCode(new OpVar((ILOpCode.Code)SimplifyShortVarOpCode(opCode), pos, nextPos, index, FindExceptionBlock(pos)));

        private void AddILOpCode(ILOpCode ilOpCode)
        {
            ilOpCode.InitStackAnalysis(_method);
            _ilOpCodes.Add(ilOpCode);
        }

        private static OpCode SimplifyShortVarOpCode(OpCode opCode)
        {
            switch (opCode)
            {
                case OpCode.Ldarga_s:
                    return OpCode.Ldarga;
                case OpCode.Ldarg_s:
                    return OpCode.Ldarg;
                case OpCode.Ldloca_s:
                    return OpCode.Ldloca;
                case OpCode.Ldloc_s:
                    return OpCode.Ldloc;
                case OpCode.Starg_s:
                    return OpCode.Starg;
                case OpCode.Stloc_s:
                    return OpCode.Stloc;
                default:
                    return opCode;
            }
        }

        private ExceptionBlock FindExceptionBlock(int pos)
        {
            ExceptionBlock xCurrentExceptionRegion = null;

            foreach (var xHandler in _method.MethodBody.ExceptionBlocks)
            {
                // todo: add support for nested handlers using a stack or so..
                if (xHandler.TryOffset >= 0)
                {
                    if (xHandler.TryOffset <= pos && (xHandler.TryLength + xHandler.TryOffset) > pos)
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
                    if (xHandler.HandlerOffset <= pos && (xHandler.HandlerOffset + xHandler.HandlerLength) > pos)
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
                if (xHandler.Kind.HasFlag(ExceptionBlockKind.Filter))
                {
                    if (xHandler.FilterOffset > 0)
                    {
                        if (xHandler.FilterOffset <= pos)
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

            return xCurrentExceptionRegion;
        }
    }
}
