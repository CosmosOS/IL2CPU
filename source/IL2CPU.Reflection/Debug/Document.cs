using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using Metadata = System.Reflection.Metadata;

namespace IL2CPU.Reflection.Debug
{
    public class Document
    {
        public string Name => _name.Value;
        public Guid Language => _language.Value;
        public ImmutableArray<byte> Hash => _hash.Value;
        public Guid HashAlgorithm => _hashAlgorithm.Value;

        private readonly MetadataReader _metadataReader;
        private readonly Metadata.Document _document;

        private readonly Lazy<string> _name;
        private readonly Lazy<Guid> _language;
        private readonly Lazy<ImmutableArray<byte>> _hash;
        private readonly Lazy<Guid> _hashAlgorithm;

        internal Document(
            MetadataReader metadataReader,
            DocumentHandle documentHandle)
        {
            _metadataReader = metadataReader;
            _document = _metadataReader.GetDocument(documentHandle);

            _name = new Lazy<string>(GetName);
            _language = new Lazy<Guid>(GetLanguage);
            _hash = new Lazy<ImmutableArray<byte>>(GetHash);
            _hashAlgorithm = new Lazy<Guid>(GetHashAlgorithm);
        }

        private string GetName() => _metadataReader.GetString(_document.Name);
        private Guid GetLanguage() => _metadataReader.GetGuid(_document.Language);
        private ImmutableArray<byte> GetHash() => _metadataReader.GetBlobContent(_document.Hash);
        private Guid GetHashAlgorithm() => _metadataReader.GetGuid(_document.HashAlgorithm);
    }
}
