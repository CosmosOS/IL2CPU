using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using IL2CPU.Reflection.Internal;

namespace IL2CPU.Reflection
{
    public class PropertyInfo : MemberInfo
    {
        public override ModuleInfo Module => ResolvedDefinition.Module;

        public override int MetadataToken => ResolvedDefinition.MetadataToken;

        public string Name => ResolvedDefinition.Name;
        public TypeInfo PropertyType => _signature.Value.ReturnType;

        public override TypeInfo DeclaringType { get; }

        public MethodInfo GetMethod => _getMethod.Value;
        public MethodInfo SetMethod => _setMethod.Value;

        public IReadOnlyList<MethodInfo> OtherAccessorMethods => _otherAccessors.Value;

        public object DefaultValue => ResolvedDefinition.DefaultValue;

        public override IReadOnlyCollection<CustomAttributeInfo> CustomAttributes => ResolvedDefinition.CustomAttributes;

        public PropertyAttributes Attributes => ResolvedDefinition.Attributes;

        #region Property attribute wrappers

        public bool HasDefault => Attributes.HasFlag(PropertyAttributes.HasDefault);
        public bool IsRTSpecialName => Attributes.HasFlag(PropertyAttributes.RTSpecialName);
        public bool IsSpecialName => Attributes.HasFlag(PropertyAttributes.SpecialName);

        #endregion

        internal ResolvedPropertyDefinition ResolvedDefinition { get; }

        private readonly Lazy<MethodSignature<TypeInfo>> _signature;

        private readonly Lazy<MethodInfo> _getMethod;
        private readonly Lazy<MethodInfo> _setMethod;

        private readonly Lazy<IReadOnlyList<MethodInfo>> _otherAccessors;

        internal PropertyInfo(
            ResolvedPropertyDefinition resolvedDefinition,
            TypeInfo declaringType)
        {
            ResolvedDefinition = resolvedDefinition;

            DeclaringType = declaringType ?? throw new Exception("Internal error!");

            _signature = new Lazy<MethodSignature<TypeInfo>>(DecodeSignature);

            _getMethod = new Lazy<MethodInfo>(ResolveGetMethod);
            _setMethod = new Lazy<MethodInfo>(ResolveSetMethod);

            _otherAccessors = new Lazy<IReadOnlyList<MethodInfo>>(ResolveOtherAccessors);
        }

        public ImmutableArray<byte> GetDefaultValueContent() => ResolvedDefinition.GetDefaultValueContent();

        private MethodSignature<TypeInfo> DecodeSignature() =>
            ResolvedDefinition.DecodeSignature(DeclaringType.GenericContext);

        private MethodInfo ResolveGetMethod() => new MethodInfo(ResolvedDefinition.GetMethod, DeclaringType, null);
        private MethodInfo ResolveSetMethod() => new MethodInfo(ResolvedDefinition.SetMethod, DeclaringType, null);

        private IReadOnlyList<MethodInfo> ResolveOtherAccessors()
        {
            var accessors = new List<MethodInfo>(ResolvedDefinition.OtherAccessorMethods.Count);

            foreach (var accessor in ResolvedDefinition.OtherAccessorMethods)
            {
                accessors.Add(new MethodInfo(accessor, DeclaringType, null));
            }

            return accessors;
        }
    }
}
