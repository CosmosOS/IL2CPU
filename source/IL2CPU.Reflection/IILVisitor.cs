using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace IL2CPU.Reflection
{
    public interface IILVisitor
    {
        void OnInlineBrTarget(ILOpCode opCode, int pos, int nextPos, int target);
        void OnInlineField(ILOpCode opCode, int pos, int nextPos, FieldInfo field);
        void OnInlineI(ILOpCode opCode, int pos, int nextPos, int value);
        void OnInlineI8(ILOpCode opCode, int pos, int nextPos, long value);
        void OnInlineMethod(ILOpCode opCode, int pos, int nextPos, MethodInfo method);
        void OnInlineNone(ILOpCode opCode, int pos, int nextPos);
        void OnInlineR(ILOpCode opCode, int pos, int nextPos, double value);
        void OnInlineSig(ILOpCode opCode, int pos, int nextPos, MethodSignature<TypeInfo> signature);
        void OnInlineString(ILOpCode opCode, int pos, int nextPos, string value);
        void OnInlineSwitch(ILOpCode opCode, int pos, int nextPos, ImmutableArray<int> branchLocations);
        void OnInlineTok(ILOpCode opCode, int pos, int nextPos, int metadataToken,
            ModuleInfo module, IReadOnlyList<TypeInfo> typeArguments, IReadOnlyList<TypeInfo> methodArguments);
        void OnInlineType(ILOpCode opCode, int pos, int nextPos, TypeInfo type);
        void OnInlineVar(ILOpCode opCode, int pos, int nextPos, ushort index);
        void OnShortInlineBrTarget(ILOpCode opCode, int pos, int nextPos, int target);
        void OnShortInlineI(ILOpCode opCode, int pos, int nextPos, byte value);
        void OnShortInlineR(ILOpCode opCode, int pos, int nextPos, float value);
        void OnShortInlineVar(ILOpCode opCode, int pos, int nextPos, ushort index);
    }
}
