using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using IL2CPU.Reflection.Debug;

namespace IL2CPU.Reflection.Internal
{
    internal class ResolvedMethodDefinition : ResolvedDefinitionBase
    {
        public override ModuleInfo Module { get; }

        public int MetadataToken => Module.MetadataReader.GetToken(_methodDefinitionHandle);

        public string Name { get; }

        public int GenericParameterCount => _methodDefinition.GetGenericParameters().Count;

        public IReadOnlyList<ParameterInfo> Parameters => _parameters.Value.Parameters;
        public ParameterInfo ReturnParameter => _parameters.Value.ReturnParameter;

        public MethodAttributes Attributes => _methodDefinition.Attributes;
        public MethodImplAttributes ImplAttributes => _methodDefinition.ImplAttributes;

        protected override CustomAttributeHandleCollection CustomAttributeHandles =>
            _methodDefinition.GetCustomAttributes();

        internal MethodBodyBlock MethodBodyBlock { get; }

        internal MethodDebugInfo DebugInfo { get; }

        private readonly MethodDefinitionHandle _methodDefinitionHandle;
        private readonly MethodDefinition _methodDefinition;

        private readonly Lazy<ParametersInfo> _parameters;

        public ResolvedMethodDefinition(
            ModuleInfo module,
            MethodDefinitionHandle methodDefinitionHandle)
        {
            Module = module;

            _methodDefinitionHandle = methodDefinitionHandle;
            _methodDefinition = Module.MetadataReader.GetMethodDefinition(_methodDefinitionHandle);

            if (_methodDefinition.RelativeVirtualAddress != 0)
            {
                MethodBodyBlock = Module.PEReader.GetMethodBody(_methodDefinition.RelativeVirtualAddress);
            }

            Name = Module.MetadataReader.GetString(_methodDefinition.Name);

            _parameters = new Lazy<ParametersInfo>(ResolveParameters);

            if (Module.PdbMetadataReader != null)
            {
                DebugInfo = new MethodDebugInfo(Module, _methodDefinitionHandle.ToDebugInformationHandle());
            }
        }

        public ResolvedMethodDefinition MakeGenericMethod(params TypeInfo[] typeArguments) => MakeGenericMethod(typeArguments);

        internal MethodSignature<TypeInfo> DecodeSignature(GenericContext genericContext) =>
            _methodDefinition.DecodeSignature(Module.TypeProvider, genericContext);

        internal IReadOnlyList<GenericParameter> ResolveGenericParameters(
            GenericContext genericContext)
        {
            var genericParameterHandles = _methodDefinition.GetGenericParameters();
            var genericParameters = new GenericParameter[genericParameterHandles.Count];

            foreach (var handle in genericParameterHandles)
            {
                var genericParameter = new GenericParameter(Module, handle, genericContext);
                genericParameters[genericParameter.Index] = genericParameter;
            }

            return genericParameters;
        }

        protected override TypeDefinitionHandle GetDeclaringTypeHandle() => _methodDefinition.GetDeclaringType();

        private ParametersInfo ResolveParameters()
        {
            var parameterHandles = _methodDefinition.GetParameters();

            var parameters = new List<ParameterInfo>(parameterHandles.Count);
            ParameterInfo returnParameter = null;

            foreach (var handle in parameterHandles)
            {
                var parameter = Module.MetadataReader.GetParameter(handle);
                var parameterInfo = new ParameterInfo(Module, handle);

                if (parameter.SequenceNumber == 0)
                {
                    returnParameter = parameterInfo;
                }
                else
                {
                    parameters.Add(parameterInfo);
                }
            }

            parameters.Sort((p1, p2) => p1.SequenceNumber.CompareTo(p2.SequenceNumber));
            return new ParametersInfo(parameters, returnParameter);
        }

        private class ParametersInfo
        {
            public IReadOnlyList<ParameterInfo> Parameters { get; }
            public ParameterInfo ReturnParameter { get; }

            public ParametersInfo(
                IReadOnlyList<ParameterInfo> parameters,
                ParameterInfo returnParameter)
            {
                Parameters = parameters;
                ReturnParameter = returnParameter;
            }
        }
    }
}
