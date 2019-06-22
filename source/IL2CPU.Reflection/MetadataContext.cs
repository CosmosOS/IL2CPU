using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

using IL2CPU.Reflection.Types;

namespace IL2CPU.Reflection
{
    public class MetadataContext : IDisposable
    {
        private const string BclAssemblyName = "mscorlib";

        public IReadOnlyCollection<AssemblyInfo> Assemblies => _assemblies.Values;

        internal AssemblyInfo BclAssembly => _bclAssembly.Value;

        private readonly IEqualityComparer<AssemblyIdentity> _assemblyIdentityEqualityComparer;

        private readonly Dictionary<AssemblyIdentity, AssemblyInfo> _assemblies;
        private readonly List<ModuleReader> _moduleReaders = new List<ModuleReader>();

        private readonly Lazy<AssemblyInfo> _bclAssembly;

        protected MetadataContext(
            IEqualityComparer<AssemblyIdentity> assemblyIdentityEqualityComparer)
        {
            _assemblyIdentityEqualityComparer = assemblyIdentityEqualityComparer ?? new DefaultAssemblyIdentityComparer();
            _assemblies = new Dictionary<AssemblyIdentity, AssemblyInfo>(_assemblyIdentityEqualityComparer);

            _bclAssembly = new Lazy<AssemblyInfo>(ResolveBclAssembly);
        }

        public AssemblyInfo ResolveAssembly(AssemblyIdentity assemblyIdentity)
        {
            if (assemblyIdentity == null)
            {
                throw new ArgumentNullException(nameof(assemblyIdentity));
            }

            if (_assemblies.TryGetValue(assemblyIdentity, out var assembly))
            {
                return assembly;
            }

            throw new FileNotFoundException("Assembly not found!", assemblyIdentity.ToString());
        }

        public AssemblyInfo ResolveFromPath(string assemblyPath)
        {
            AssemblyIdentity assemblyIdentity;

            using (var stream = File.OpenRead(assemblyPath))
            {
                using (var peReader = new PEReader(stream))
                {
                    var metadataReader = peReader.GetMetadataReader();
                    var definition = metadataReader.GetAssemblyDefinition();

                    assemblyIdentity = AssemblyIdentity.FromAssemblyDefinition(metadataReader, definition);
                }
            }

            if (_assemblies.TryGetValue(assemblyIdentity, out var assembly))
            {
                return assembly;
            }
            
            return null;
        }

        public DefinedType GetBclType(BclType bclType)
        {
            if (bclType == null)
            {
                throw new ArgumentNullException(nameof(bclType));
            }

            return new BclTypeReference(this, bclType.Namespace, bclType.Name);
        }

        public TypeInfo ResolveTypeByName(string name, bool throwOnError = false) =>
            ResolveTypeByNameInternal(name, throwOnError);

        internal bool TryParseType(string typeString, out string typeName, out AssemblyIdentity assemblyIdentity)
        {
            var parts = typeString.Split(',');

            if (parts.Length == 0)
            {
                typeName = null;
                assemblyIdentity = null;

                return false;
            }
            else
            {
                typeName = parts[0];

                if (parts.Length == 1)
                {
                    assemblyIdentity = null;
                    return true;
                }
                else
                {
                    assemblyIdentity = AssemblyIdentity.Parse(typeString.Substring(typeString.IndexOf(',') + 1).Trim());
                    return true;
                }
            }
        }

        internal DefinedType ResolveTypeByNameInternal(
            string name, bool throwOnError = false, AssemblyInfo assembly = null, char nestedTypeSeparator = '+')
        {
            if (TryParseType(name, out var fullTypeName, out var assemblyIdentity))
            {
                if (assemblyIdentity != null)
                {
                    assembly = ResolveAssembly(assemblyIdentity);
                }

                var lastNestedTypeSeparatorIndex = fullTypeName.LastIndexOf(nestedTypeSeparator);

                if (lastNestedTypeSeparatorIndex != -1)
                {
                    var parentType = ResolveTypeByNameInternal(
                        fullTypeName.Substring(0, lastNestedTypeSeparatorIndex), throwOnError, assembly, nestedTypeSeparator);

                    return parentType.ResolveNestedType(fullTypeName.Substring(lastNestedTypeSeparatorIndex + 1));
                }

                var lastDotIndex = fullTypeName.LastIndexOf('.');

                string typeNamespace = String.Empty;
                string typeName = String.Empty;

                if (lastDotIndex != -1)
                {
                    typeNamespace = fullTypeName.Substring(0, lastDotIndex);
                    typeName = fullTypeName.Substring(lastDotIndex + 1);
                }
                else
                {
                    typeName = fullTypeName;
                }

                if (assemblyIdentity == null)
                {
                    return assembly?.ResolveType(typeNamespace, typeName)
                        ?? BclAssembly?.ResolveExportedType(typeNamespace, typeName);
                }
                else
                {
                    if (assembly != null)
                    {
                        return assembly.ResolveType(typeNamespace, typeName);
                    }

                    throw new InvalidOperationException();
                }
            }

            if (throwOnError)
            {
                throw new ArgumentOutOfRangeException(nameof(name));
            }

            return null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal AssemblyInfo ResolveAssemblyReference(
            MetadataReader metadataReader,
            AssemblyReference assemblyReference) =>
            ResolveAssembly(AssemblyIdentity.FromAssemblyReference(metadataReader, assemblyReference));

        internal Stream ResolveFile(AssemblyInfo assembly, string name)
        {
            var assemblyPath = assembly.ManifestModule.Location;

            if (File.Exists(assemblyPath))
            {
                var directory = Path.GetDirectoryName(assemblyPath);
                var filePath = Path.Combine(directory, name);

                if (File.Exists(filePath))
                {
                    return File.OpenRead(filePath);
                }
            }

            throw new NotImplementedException();
        }

        internal ModuleInfo ResolveModule(AssemblyInfo assembly, string name)
        {
            var assemblyPath = assembly.ManifestModule.Location;

            if (File.Exists(assemblyPath))
            {
                var directory = Path.GetDirectoryName(assemblyPath);
                var filePath = Path.Combine(directory, name);

                if (File.Exists(filePath))
                {
#pragma warning disable CA2000 // Dispose objects before losing scope
                    var peReader = new PEReader(File.OpenRead(filePath));
#pragma warning restore CA2000 // Dispose objects before losing scope
                    var moduleReader = new ModuleReader(peReader, filePath);

                    _moduleReaders.Add(moduleReader);

                    return new ModuleInfo(this, assembly, moduleReader);
                }
            }

            throw new NotImplementedException();
        }

        protected virtual AssemblyInfo ResolveBclAssembly()
        {
            foreach (var assembly in _assemblies)
            {
                if (assembly.Key.Name == BclAssemblyName)
                {
                    return assembly.Value;
                }
            }

            return null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var moduleReader in _moduleReaders)
                {
                    moduleReader.Dispose();
                }
            }
        }

        public static MetadataContext FromAssemblyPaths(
            IEnumerable<string> assemblyPaths,
            IEqualityComparer<AssemblyIdentity> assemblyIdentityEqualityComparer = null)
        {
            var metadataContext = new MetadataContext(assemblyIdentityEqualityComparer);

            foreach (var assemblyPath in assemblyPaths)
            {
                if (!File.Exists(assemblyPath))
                {
                    throw new FileNotFoundException("Assembly file path not found!", assemblyPath);
                }

#pragma warning disable CA2000 // Dispose objects before losing scope
                var peReader = new PEReader(File.OpenRead(assemblyPath), PEStreamOptions.LeaveOpen);
#pragma warning restore CA2000 // Dispose objects before losing scope

                if (!peReader.HasMetadata)
                {
                    throw new BadImageFormatException("Assembly file doesn't contain metadata!", assemblyPath);
                }

                var moduleReader = new ModuleReader(peReader, assemblyPath);

                if (!moduleReader.MetadataReader.IsAssembly)
                {
                    throw new InvalidOperationException("Assembly file is not the manifest module!");
                }

                var metadataReader = moduleReader.MetadataReader;

                var assemblyDefinition = metadataReader.GetAssemblyDefinition();
                var assemblyIdentity = new AssemblyIdentity(
                    metadataReader.GetString(assemblyDefinition.Name),
                    assemblyDefinition.Version,
                    metadataReader.GetString(assemblyDefinition.Culture),
                    metadataReader.GetBlobContent(assemblyDefinition.PublicKey));

                if (metadataContext._assemblies.ContainsKey(assemblyIdentity))
                {
                    continue;
                }

                metadataContext._assemblies.Add(
                    assemblyIdentity,
                    new AssemblyInfo(metadataContext, assemblyIdentity, moduleReader));

                metadataContext._moduleReaders.Add(moduleReader);
            }

            return metadataContext;
        }
    }
}
