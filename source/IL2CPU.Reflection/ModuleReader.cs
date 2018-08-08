using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace IL2CPU.Reflection
{
    internal sealed class ModuleReader : IDisposable
    {
        public PEReader PEReader { get; }

        public MetadataReader MetadataReader => _metadataReader.Value;
        public MetadataReader PdbMetadataReader => _pdb.Value?.MetadataReader;

        public string ModulePath { get; }
        public string PdbPath => _pdb.Value.PdbPath;

        private readonly Lazy<MetadataReader> _metadataReader;
        private readonly Lazy<Pdb> _pdb;

        public ModuleReader(PEReader peReader, string modulePath = null)
        {
            _metadataReader = new Lazy<MetadataReader>(GetModuleReader);
            _pdb = new Lazy<Pdb>(ResolvePdb);

            PEReader = peReader;
            ModulePath = modulePath;
        }

        public void Dispose()
        {
            PEReader.Dispose();
            
            if (_pdb.IsValueCreated)
            {
                _pdb.Value?.Dispose();
            }
        }

        private MetadataReader GetModuleReader() => PEReader.GetMetadataReader(MetadataReaderOptions.None);

        private Pdb ResolvePdb()
        {
            if (ModulePath == null)
            {
                return null;
            }

#pragma warning disable CA2000 // Dispose objects before losing scope
            if (PEReader.TryOpenAssociatedPortablePdb(
                ModulePath, path => File.OpenRead(path), out var pdbReaderProvider, out var pdbPath))
#pragma warning restore CA2000 // Dispose objects before losing scope
            {
                return new Pdb(pdbReaderProvider, pdbPath);
            }

            return null;
        }

        private class Pdb : IDisposable
        {
            public MetadataReader MetadataReader => _metadataReader.Value;
            public string PdbPath { get; }

            private readonly MetadataReaderProvider _pdbReaderProvider;

            private readonly Lazy<MetadataReader> _metadataReader;

            public Pdb(
                MetadataReaderProvider pdbReaderProvider,
                string pdbPath = null)
            {
                _pdbReaderProvider = pdbReaderProvider;
                _metadataReader = new Lazy<MetadataReader>(GetMetadataReader);

                PdbPath = pdbPath;
            }

            public void Dispose() => _pdbReaderProvider.Dispose();

            private MetadataReader GetMetadataReader() =>
                _pdbReaderProvider.GetMetadataReader(MetadataReaderOptions.None);
        }
    }
}
