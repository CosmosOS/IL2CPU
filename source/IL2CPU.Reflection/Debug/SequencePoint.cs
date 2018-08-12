using System;
using System.Reflection.Metadata;
using Metadata = System.Reflection.Metadata;

namespace IL2CPU.Reflection.Debug
{
    public class SequencePoint
    {
        public Document Document => _document.Value;

        public int Offset => _sequencePoint.Offset;
        
        public int StartLine => _sequencePoint.StartLine;
        public int EndLine => _sequencePoint.EndLine;

        public int StartColumn => _sequencePoint.StartColumn;
        public int EndColumn => _sequencePoint.EndColumn;

        public bool IsHidden => _sequencePoint.IsHidden;

        private readonly MetadataReader _metadataReader;
        private readonly Metadata.SequencePoint _sequencePoint;

        private readonly Lazy<Document> _document;

        internal SequencePoint(
            MetadataReader metadataReader,
            Metadata.SequencePoint sequencePoint)
        {
            _metadataReader = metadataReader;
            _sequencePoint = sequencePoint;

            _document = new Lazy<Document>(GetDocument);
        }

        private Document GetDocument() => new Document(_metadataReader, _sequencePoint.Document);
    }
}
