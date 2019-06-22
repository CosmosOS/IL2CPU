using System;
using System.Collections.Generic;
using System.Reflection;

using IL2CPU.Reflection.Internal;

namespace IL2CPU.Reflection
{
    public class EventInfo : MemberInfo
    {
        public override ModuleInfo Module { get; }

        public override int MetadataToken => ResolvedDefinition.MetadataToken;

        public string Name => ResolvedDefinition.Name;
        public TypeInfo EventType => _eventType.Value;

        public override TypeInfo DeclaringType { get; }

        public MethodInfo AddMethod => _addMethod.Value;
        public MethodInfo RaiseMethod => _raiseMethod.Value;
        public MethodInfo RemoveMethod => _removeMethod.Value;

        public IReadOnlyList<MethodInfo> OtherAccessorMethods => _otherAccessorMethods.Value;

        public override IReadOnlyCollection<CustomAttributeInfo> CustomAttributes => ResolvedDefinition.CustomAttributes;

        public EventAttributes Attributes => ResolvedDefinition.Attributes;

        #region Event attribute wrappers

        public bool IsRTSpecialName => Attributes.HasFlag(EventAttributes.RTSpecialName);
        public bool IsSpecialName => Attributes.HasFlag(EventAttributes.SpecialName);

        #endregion

        internal ResolvedEventDefinition ResolvedDefinition { get; }

        private readonly Lazy<TypeInfo> _eventType;

        private readonly Lazy<MethodInfo> _addMethod;
        private readonly Lazy<MethodInfo> _raiseMethod;
        private readonly Lazy<MethodInfo> _removeMethod;

        private readonly Lazy<IReadOnlyList<MethodInfo>> _otherAccessorMethods;

        internal EventInfo(
            ResolvedEventDefinition resolvedDefinition,
            TypeInfo declaringType)
        {
            ResolvedDefinition = resolvedDefinition;

            DeclaringType = declaringType ?? throw new Exception("Internal error!");

            _eventType = new Lazy<TypeInfo>(ResolveEventType);

            _addMethod = new Lazy<MethodInfo>(GetAddMethod);
            _raiseMethod = new Lazy<MethodInfo>(GetRaiseMethod);
            _removeMethod = new Lazy<MethodInfo>(GetRemoveMethod);

            _otherAccessorMethods = new Lazy<IReadOnlyList<MethodInfo>>(GetOtherEventAccessorMethods);
        }

        private TypeInfo ResolveEventType() =>
            ResolvedDefinition.ResolveEventType(DeclaringType.GenericContext);

        private MethodInfo GetAddMethod() => new MethodInfo(ResolvedDefinition.AddMethod, DeclaringType, null);
        private MethodInfo GetRaiseMethod() => new MethodInfo(ResolvedDefinition.RaiseMethod, DeclaringType, null);
        private MethodInfo GetRemoveMethod() => new MethodInfo(ResolvedDefinition.RemoveMethod, DeclaringType, null);

        private IReadOnlyList<MethodInfo> GetOtherEventAccessorMethods()
        {
            var methods = new List<MethodInfo>(ResolvedDefinition.OtherAccessorMethods.Count);

            foreach (var accessor in ResolvedDefinition.OtherAccessorMethods)
            {
                methods.Add(new MethodInfo(accessor, DeclaringType, null));
            }

            return methods;
        }
    }
}
