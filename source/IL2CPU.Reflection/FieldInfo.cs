using System;
using System.Collections.Generic;
using System.Reflection;

using IL2CPU.Reflection.Internal;

namespace IL2CPU.Reflection
{
    public class FieldInfo : MemberInfo
    {
        public override ModuleInfo Module => ResolvedDefinition.Module;

        public override int MetadataToken => ResolvedDefinition.MetadataToken;

        public string Name => ResolvedDefinition.Name;
        public TypeInfo FieldType => _fieldType.Value;

        public override TypeInfo DeclaringType { get; }

        public object DefaultValue => ResolvedDefinition.DefaultValue;
        public int Offset => ResolvedDefinition.Offset;

        public override IReadOnlyCollection<CustomAttributeInfo> CustomAttributes => ResolvedDefinition.CustomAttributes;

        public FieldAttributes Attributes => ResolvedDefinition.Attributes;

        #region Field attribute wrappers

        public bool IsAssembly => HasFlag(FieldAttributes.FieldAccessMask, FieldAttributes.Assembly);
        public bool HasDefault => HasFlag(FieldAttributes.HasDefault);
        public bool IsFamANDAssem => HasFlag(FieldAttributes.FieldAccessMask, FieldAttributes.FamANDAssem);
        public bool IsFamily => HasFlag(FieldAttributes.FieldAccessMask, FieldAttributes.Family);
        public bool IsFamORAssem => HasFlag(FieldAttributes.FieldAccessMask, FieldAttributes.FamORAssem);
        public bool HasFieldMarshal => HasFlag(FieldAttributes.HasFieldMarshal);
        public bool HasFieldRVA => HasFlag(FieldAttributes.HasFieldRVA);
        public bool IsInitOnly => HasFlag(FieldAttributes.InitOnly);
        public bool IsLiteral => HasFlag(FieldAttributes.Literal);
        public bool IsPinvokeImpl => HasFlag(FieldAttributes.PinvokeImpl);
        public bool IsPrivate => HasFlag(FieldAttributes.FieldAccessMask, FieldAttributes.Private);
        public bool IsPrivateScope => HasFlag(FieldAttributes.FieldAccessMask, FieldAttributes.PrivateScope);
        public bool IsPublic => HasFlag(FieldAttributes.FieldAccessMask, FieldAttributes.Public);
        public bool IsRTSpecialName => HasFlag(FieldAttributes.RTSpecialName);
        public bool IsSpecialName => HasFlag(FieldAttributes.SpecialName);
        public bool IsStatic => HasFlag(FieldAttributes.Static);

        #endregion

        internal ResolvedFieldDefinition ResolvedDefinition { get; }

        private readonly Lazy<TypeInfo> _fieldType;

        internal FieldInfo(
            ResolvedFieldDefinition resolvedDefinition,
            TypeInfo declaringType)
        {
            ResolvedDefinition = resolvedDefinition;

            DeclaringType = declaringType;

            _fieldType = new Lazy<TypeInfo>(DecodeFieldType);
        }

        public byte[] GetDefaultValueBytes() => ResolvedDefinition.GetDefaultValueBytes();
        public void GetRvaBytes(byte[] buffer) => ResolvedDefinition.GetRvaBytes(buffer);

        public override string ToString()
        {
            // https://github.com/dotnet/coreclr/blob/0fbd855e38bc3ec269479b5f6bf561dcfd67cbb6/src/System.Private.CoreLib/src/System/RtType.cs

            if (FieldType.Namespace == null
                || FieldType.IsPrimitive
                || FieldType.IsBclType(BclType.Void)
                || FieldType.IsBclType(BclType.TypedReference))
            {
                return $"{FieldType.Name} {Name}";
            }
            else
            {
                return $"{FieldType.Namespace}.{FieldType.Name} {Name}";
            }
        }

        internal bool Matches(string name, TypeInfo signature) => Name == name && FieldType == signature;

        private TypeInfo DecodeFieldType() =>
            ResolvedDefinition.DecodeSignature(DeclaringType.GenericContext);

        private bool HasFlag(FieldAttributes flag) => (Attributes & flag) != 0;
        private bool HasFlag(FieldAttributes mask, FieldAttributes flag) => (Attributes & mask) == flag;
    }
}
