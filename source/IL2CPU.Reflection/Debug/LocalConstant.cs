using System;
using System.Reflection.Metadata;

namespace IL2CPU.Reflection.Debug
{
    public class LocalConstant
    {
        public string Name => _name.Value;

        private MetadataReader _metadataReader;

        private LocalConstantHandle _localConstantHandle;
        private System.Reflection.Metadata.LocalConstant _localConstant;

        private Lazy<string> _name;

        internal LocalConstant(
            MetadataReader metadataReader,
            LocalConstantHandle localConstantHandle)
        {
            _metadataReader = metadataReader;

            _localConstantHandle = localConstantHandle;
            _localConstant = _metadataReader.GetLocalConstant(_localConstantHandle);

            _name = new Lazy<string>(GetName);
        }

        private string GetName() => _metadataReader.GetString(_localConstant.Name);
    }
}
