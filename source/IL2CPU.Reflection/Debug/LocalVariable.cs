using System;
using System.Reflection.Metadata;

namespace IL2CPU.Reflection.Debug
{
    public class LocalVariable
    {
        public bool IsCompilerGenerated =>
            _localVariable.Attributes == LocalVariableAttributes.DebuggerHidden;

        public int Index => _localVariable.Index;
        public string Name => _name.Value;

        public LocalVariableAttributes Attributes => _localVariable.Attributes;

        private MetadataReader _metadataReader;

        private LocalVariableHandle _localVariableHandle;
        private System.Reflection.Metadata.LocalVariable _localVariable;

        private Lazy<string> _name;

        internal LocalVariable(
            MetadataReader metadataReader,
            LocalVariableHandle localVariableHandle)
        {
            _metadataReader = metadataReader;

            _localVariableHandle = localVariableHandle;
            _localVariable = _metadataReader.GetLocalVariable(_localVariableHandle);

            _name = new Lazy<string>(GetName);
        }

        private string GetName() => _metadataReader.GetString(_localVariable.Name);
    }
}
