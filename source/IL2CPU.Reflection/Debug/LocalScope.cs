using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using Metadata = System.Reflection.Metadata;

namespace IL2CPU.Reflection.Debug
{
    public class LocalScope
    {
        public int StartOffset => _localScope.StartOffset;
        public int EndOffset => _localScope.EndOffset;

        public int Length => _localScope.Length;

        public IReadOnlyList<LocalConstant> LocalConstants => _localConstants.Value;
        public IReadOnlyList<LocalVariable> LocalVariables => _localVariables.Value;

        public IReadOnlyList<LocalScope> ChildrenScopes => _childrenScopes.Value;

        private readonly MetadataReader _metadataReader;
        private readonly Metadata.LocalScope _localScope;

        private readonly Lazy<IReadOnlyList<LocalConstant>> _localConstants;
        private readonly Lazy<IReadOnlyList<LocalVariable>> _localVariables;

        private readonly Lazy<IReadOnlyList<LocalScope>> _childrenScopes;

        internal LocalScope(
            MetadataReader metadataReader,
            LocalScopeHandle localScopeHandle)
        {
            _metadataReader = metadataReader;
            _localScope = _metadataReader.GetLocalScope(localScopeHandle);

            _localConstants = new Lazy<IReadOnlyList<LocalConstant>>(ResolveLocalConstants);
            _localVariables = new Lazy<IReadOnlyList<LocalVariable>>(ResolveLocalVariables);

            _childrenScopes = new Lazy<IReadOnlyList<LocalScope>>(ResolveChildrenScopes);
        }

        private IReadOnlyList<LocalConstant> ResolveLocalConstants()
        {
            var localConstantHandles = _localScope.GetLocalConstants();
            var localConstants = new List<LocalConstant>(localConstantHandles.Count);

            foreach (var handle in localConstantHandles)
            {
                localConstants.Add(new LocalConstant(_metadataReader, handle));
            }

            return localConstants;
        }

        private IReadOnlyList<LocalVariable> ResolveLocalVariables()
        {
            var localVariableHandles = _localScope.GetLocalVariables();
            var localVariables = new List<LocalVariable>(localVariableHandles.Count);

            foreach (var handle in localVariableHandles)
            {
                localVariables.Add(new LocalVariable(_metadataReader, handle));
            }

            return localVariables;
        }

        private IReadOnlyList<LocalScope> ResolveChildrenScopes()
        {
            var childrenScopes = new List<LocalScope>();
            var childrenScopeHandlesEnumerator = _localScope.GetChildren();

            while (childrenScopeHandlesEnumerator.MoveNext())
            {
                childrenScopes.Add(
                    new LocalScope(_metadataReader, childrenScopeHandlesEnumerator.Current));
            }

            return childrenScopes;
        }
    }
}
