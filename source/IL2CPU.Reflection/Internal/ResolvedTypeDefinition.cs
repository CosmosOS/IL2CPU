using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace IL2CPU.Reflection.Internal
{
    internal class ResolvedTypeDefinition : ResolvedDefinitionBase
    {
        public override ModuleInfo Module { get; }

        public int MetadataToken => Module.MetadataReader.GetToken(_typeDefinitionHandle);

        public string Namespace { get; }
        public string Name { get; }

        public int GenericParameterCount => _typeDefinition.GetGenericParameters().Count;

        public IReadOnlyList<ResolvedTypeDefinition> NestedTypes => _nestedTypes.Value;

        public IReadOnlyList<ResolvedEventDefinition> Events => _events.Value;
        public IReadOnlyList<ResolvedFieldDefinition> Fields => _fields.Value;
        public IReadOnlyList<ResolvedMethodDefinition> Methods => _methods.Value;
        public IReadOnlyList<ResolvedPropertyDefinition> Properties => _properties.Value;

        public TypeAttributes Attributes => _typeDefinition.Attributes;

        protected override CustomAttributeHandleCollection CustomAttributeHandles =>
            _typeDefinition.GetCustomAttributes();

        private readonly TypeDefinitionHandle _typeDefinitionHandle;
        private readonly TypeDefinition _typeDefinition;

        private readonly Lazy<IReadOnlyList<ResolvedTypeDefinition>> _nestedTypes;

        private readonly Lazy<IReadOnlyList<ResolvedEventDefinition>> _events;
        private readonly Lazy<IReadOnlyList<ResolvedFieldDefinition>> _fields;
        private readonly Lazy<IReadOnlyList<ResolvedMethodDefinition>> _methods;
        private readonly Lazy<IReadOnlyList<ResolvedPropertyDefinition>> _properties;

        public ResolvedTypeDefinition(
            ModuleInfo module,
            TypeDefinitionHandle typeDefinitionHandle)
        {
            Module = module;

            _typeDefinitionHandle = typeDefinitionHandle;
            _typeDefinition = Module.MetadataReader.GetTypeDefinition(typeDefinitionHandle);

            Namespace = Module.MetadataReader.GetString(_typeDefinition.Namespace);

            if (String.IsNullOrEmpty(Namespace))
            {
                Namespace = null;
            }

            Name = Module.MetadataReader.GetString(_typeDefinition.Name);

            _nestedTypes = new Lazy<IReadOnlyList<ResolvedTypeDefinition>>(ResolveNestedTypes);

            _events = new Lazy<IReadOnlyList<ResolvedEventDefinition>>(ResolveEvents);
            _fields = new Lazy<IReadOnlyList<ResolvedFieldDefinition>>(ResolveFields);
            _methods = new Lazy<IReadOnlyList<ResolvedMethodDefinition>>(ResolveMethods);
            _properties = new Lazy<IReadOnlyList<ResolvedPropertyDefinition>>(ResolveProperties);
        }

        internal TypeInfo ResolveBaseType(GenericContext genericContext)
        {
            var handle = _typeDefinition.BaseType;

            if (handle.IsNil)
            {
                return null;
            }

            return Module.ResolveTypeHandle(_typeDefinition.BaseType, genericContext);
        }

        internal IReadOnlyList<GenericParameter> ResolveGenericParameters(
            GenericContext genericContext)
        {
            var genericParameterHandles = _typeDefinition.GetGenericParameters();
            var genericParameters = new GenericParameter[genericParameterHandles.Count];

            foreach (var handle in genericParameterHandles)
            {
                var genericParameter = new GenericParameter(Module, handle, genericContext);
                genericParameters[genericParameter.Index] = genericParameter;
            }

            return genericParameters;
        }

        internal IReadOnlyCollection<TypeInfo> ResolveImplementedInterfaces(
            GenericContext genericContext)
        {
            var handles = _typeDefinition.GetInterfaceImplementations();
            var interfaces = new List<TypeInfo>(handles.Count);

            foreach (var handle in handles)
            {
                interfaces.Add(Module.ResolveTypeHandle(
                    Module.MetadataReader.GetInterfaceImplementation(handle).Interface, genericContext));
            }

            return interfaces;
        }

        internal IReadOnlyCollection<MethodImpl> ResolveMethodImplementations(
            GenericContext genericContext)
        {
            var handles = _typeDefinition.GetMethodImplementations();
            var methodImpls = new List<MethodImpl>(handles.Count);

            foreach (var handle in handles)
            {
                methodImpls.Add(new MethodImpl(Module, genericContext, handle));
            }

            return methodImpls;
        }

        internal TypeLayout GetClassLayout() =>_typeDefinition.GetLayout();

        protected override TypeDefinitionHandle GetDeclaringTypeHandle() => _typeDefinition.GetDeclaringType();

        private IReadOnlyList<ResolvedTypeDefinition> ResolveNestedTypes()
        {
            var handles = _typeDefinition.GetNestedTypes();
            var types = new List<ResolvedTypeDefinition>(handles.Length);

            foreach (var handle in handles)
            {
                types.Add(Module.ResolveTypeDefinitionInternal(handle));
            }

            return types;
        }

        private IReadOnlyList<ResolvedEventDefinition> ResolveEvents()
        {
            var eventHandles = _typeDefinition.GetEvents();
            var events = new List<ResolvedEventDefinition>(eventHandles.Count);

            foreach (var handle in eventHandles)
            {
                events.Add(Module.ResolveEventDefinitionInternal(handle));
            }

            return events;
        }

        private IReadOnlyList<ResolvedFieldDefinition> ResolveFields()
        {
            var fieldHandles = _typeDefinition.GetFields();
            var fields = new List<ResolvedFieldDefinition>(fieldHandles.Count);

            foreach (var handle in fieldHandles)
            {
                fields.Add(Module.ResolveFieldDefinitionInternal(handle));
            }

            return fields;
        }

        private IReadOnlyList<ResolvedMethodDefinition> ResolveMethods()
        {
            var methodHandles = _typeDefinition.GetMethods();
            var methods = new List<ResolvedMethodDefinition>(methodHandles.Count);

            foreach (var handle in methodHandles)
            {
                methods.Add(Module.ResolveMethodDefinitionInternal(handle));
            }

            return methods;
        }

        private IReadOnlyList<ResolvedPropertyDefinition> ResolveProperties()
        {
            var propertyHandles = _typeDefinition.GetProperties();
            var properties = new List<ResolvedPropertyDefinition>(propertyHandles.Count);

            foreach (var handle in propertyHandles)
            {
                properties.Add(Module.ResolvePropertyDefinitionInternal(handle));
            }

            return properties;
        }
    }
}
