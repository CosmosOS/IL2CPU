using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace IL2CPU.Reflection.Internal
{
    internal class ResolvedEventDefinition : ResolvedDefinitionBase
    {
        public override ModuleInfo Module { get; }

        public int MetadataToken => Module.MetadataReader.GetToken(_eventDefinitionHandle);

        public string Name { get; }

        public ResolvedMethodDefinition AddMethod => _addMethod.Value;
        public ResolvedMethodDefinition RaiseMethod => _raiseMethod.Value;
        public ResolvedMethodDefinition RemoveMethod => _removeMethod.Value;

        public IReadOnlyList<ResolvedMethodDefinition> OtherAccessorMethods => _otherAccessorMethods.Value;

        public EventAttributes Attributes => _eventDefinition.Attributes;

        protected override CustomAttributeHandleCollection CustomAttributeHandles =>
            _eventDefinition.GetCustomAttributes();

        private readonly EventDefinitionHandle _eventDefinitionHandle;
        private readonly EventDefinition _eventDefinition;

        private readonly EventAccessors _eventAccessors;

        private readonly Lazy<ResolvedMethodDefinition> _addMethod;
        private readonly Lazy<ResolvedMethodDefinition> _raiseMethod;
        private readonly Lazy<ResolvedMethodDefinition> _removeMethod;

        private readonly Lazy<IReadOnlyList<ResolvedMethodDefinition>> _otherAccessorMethods;

        public ResolvedEventDefinition(
            ModuleInfo module,
            EventDefinitionHandle eventDefinitionHandle)
        {
            Module = module;

            _eventDefinitionHandle = eventDefinitionHandle;
            _eventDefinition = Module.MetadataReader.GetEventDefinition(_eventDefinitionHandle);

            _eventAccessors = _eventDefinition.GetAccessors();

            Name = Module.MetadataReader.GetString(_eventDefinition.Name);

            _addMethod = new Lazy<ResolvedMethodDefinition>(GetAddMethod);
            _raiseMethod = new Lazy<ResolvedMethodDefinition>(GetRaiseMethod);
            _removeMethod = new Lazy<ResolvedMethodDefinition>(GetRemoveMethod);

            _otherAccessorMethods = new Lazy<IReadOnlyList<ResolvedMethodDefinition>>(GetOtherEventAccessorMethods);
        }

        internal TypeInfo ResolveEventType(GenericContext genericContext) =>
            Module.ResolveTypeHandle(_eventDefinition.Type, genericContext);

        private ResolvedMethodDefinition GetAddMethod() => Module.ResolveMethodDefinitionInternal(_eventAccessors.Adder);
        private ResolvedMethodDefinition GetRaiseMethod() => Module.ResolveMethodDefinitionInternal(_eventAccessors.Raiser);
        private ResolvedMethodDefinition GetRemoveMethod() => Module.ResolveMethodDefinitionInternal(_eventAccessors.Remover);

        private IReadOnlyList<ResolvedMethodDefinition> GetOtherEventAccessorMethods()
        {
            var methods = new List<ResolvedMethodDefinition>(_eventAccessors.Others.Length);

            foreach (var handle in _eventAccessors.Others)
            {
                methods.Add(Module.ResolveMethodDefinitionInternal(handle));
            }

            return methods;
        }
    }
}
