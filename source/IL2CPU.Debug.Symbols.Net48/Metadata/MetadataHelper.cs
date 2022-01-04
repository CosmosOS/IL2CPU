using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace IL2CPU.Debug.Symbols.Metadata
{
    internal static class MetadataHelper
    {
        private static MetadataStringDecoder mMetadataStringDecoder;
        private static readonly Dictionary<string, MetadataReaderProvider> mMetadataCache = new Dictionary<string, MetadataReaderProvider>();

        public static MetadataReader TryGetReader(string aAssemblyPath)
        {
            if (mMetadataCache.TryGetValue(aAssemblyPath, out var provider))
            {
                return provider.GetMetadataReader();
            }

            provider = TryOpenReaderFromAssemblyFile(aAssemblyPath);

            if (provider == null)
            {
                return null;
            }

            mMetadataCache.Add(aAssemblyPath, provider);

            // The reader has already been open, so this doesn't throw:
            return provider.GetMetadataReader();
        }

        public static Type GetTypeFromReference(MetadataReader reader, Module aModule, TypeReferenceHandle handle, byte rawTypeKind)
        {
            int xToken = MetadataTokens.GetToken(handle);
            return aModule.ResolveType(xToken, null, null);
        }

        public static MetadataStringDecoder GetMetadataStringDecoder()
        {
            if (mMetadataStringDecoder == null)
            {
                mMetadataStringDecoder = new CachingMetadataStringDecoder(0x10000); // TODO: Tune the size
            }
            return mMetadataStringDecoder;
        }

        private static PEReader TryGetPEReader(string aAssemblyPath)
        {
            var peStream = TryOpenFile(aAssemblyPath);
            if (peStream != null)
            {
                return new PEReader(peStream);
            }

            return null;
        }

        private static MetadataReaderProvider TryOpenReaderFromAssemblyFile(string aAssemblyPath)
        {
            using (var peReader = TryGetPEReader(aAssemblyPath))
            {
                if (peReader == null)
                {
                    return null;
                }

                if (peReader.TryOpenAssociatedPortablePdb(aAssemblyPath, TryOpenFile, out var provider, out var pdbPath))
                {
                    return provider;
                }
            }

            return null;
        }

        private static Stream TryOpenFile(string aPath)
        {
            if (!File.Exists(aPath))
            {
                return null;
            }

            try
            {
                return File.OpenRead(aPath);
            }
            catch
            {
                return null;
            }
        }
    }
}
