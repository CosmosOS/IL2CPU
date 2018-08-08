using System;
using System.Collections.Generic;
using System.Reflection.Metadata;

namespace IL2CPU.Reflection.Debug
{
    public class MethodDebugInfo
    {
        public MethodInfo StateMachineKickOffMethod => _stateMachineKickOffMethod.Value;

        public Document Document => _document.Value;
        public IReadOnlyList<SequencePoint> SequencePoints => _sequencePoints.Value;

        private readonly ModuleInfo _module;
        private readonly MetadataReader _pdbMetadataReader;

        private readonly MethodDebugInformationHandle _methodDebugInformationHandle;
        private readonly MethodDebugInformation _methodDebugInformation;

        private readonly Lazy<Document> _document;

        private readonly Lazy<MethodInfo> _stateMachineKickOffMethod;

        private readonly Lazy<IReadOnlyList<LocalScope>> _localScopes;
        private readonly Lazy<IReadOnlyList<SequencePoint>> _sequencePoints;

        internal MethodDebugInfo(
            ModuleInfo module,
            MethodDebugInformationHandle methodDebugInformationHandle)
        {
            _module = module;
            _pdbMetadataReader = module.PdbMetadataReader;

            _methodDebugInformationHandle = methodDebugInformationHandle;
            _methodDebugInformation = _pdbMetadataReader.GetMethodDebugInformation(_methodDebugInformationHandle);

            _document = new Lazy<Document>(GetDocument);

            _stateMachineKickOffMethod = new Lazy<MethodInfo>(ResolveStateMachineKickOffMethod);

            _localScopes = new Lazy<IReadOnlyList<LocalScope>>(ResolveLocalScopes);
            _sequencePoints = new Lazy<IReadOnlyList<SequencePoint>>(ResolveSequencePoints);
        }

        private Document GetDocument() => new Document(_pdbMetadataReader, _methodDebugInformation.Document);

        private MethodInfo ResolveStateMachineKickOffMethod()
        {
            var metadataReader = _module.MetadataReader;
            var methodDefinitionHandle = _methodDebugInformation.GetStateMachineKickoffMethod();
            
            return _module.ResolveMethodDefinition(methodDefinitionHandle);
        }

        private IReadOnlyList<LocalScope> ResolveLocalScopes()
        {
            var debugInfoLocalScopes = _pdbMetadataReader.GetLocalScopes(_methodDebugInformationHandle);
            var localScopes = new List<LocalScope>(debugInfoLocalScopes.Count);

            foreach (var handle in debugInfoLocalScopes)
            {
                localScopes.Add(new LocalScope(_pdbMetadataReader, handle));
            }

            return localScopes;
        }

        private IReadOnlyList<SequencePoint> ResolveSequencePoints()
        {
            var debugInfoSequencePoints = _methodDebugInformation.GetSequencePoints();
            var sequencePoints = new List<SequencePoint>();

            foreach (var sequencePoint in debugInfoSequencePoints)
            {
                sequencePoints.Add(new SequencePoint(_pdbMetadataReader, sequencePoint));
            }

            return sequencePoints;
        }
    }
}
