using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using IL2CPU.Reflection.Internal;
using IL2CPU.Reflection.Types;

namespace IL2CPU.Reflection
{
    public sealed class AssemblyInfo : ICustomAttributeProvider
    {
        public AssemblyIdentity Identity { get; }

        public ModuleInfo ManifestModule { get; }

        public IReadOnlyList<ModuleInfo> Modules => _modules.Value;
        public IReadOnlyList<DefinedType> ExportedTypes => _exportedTypes.Value;

        public IReadOnlyCollection<ManifestResource> ManifestResources => _manifestResources.Value;

        public IReadOnlyCollection<AssemblyIdentity> ReferencedAssemblies => _referencedAssemblies.Value;

        public IReadOnlyCollection<CustomAttributeInfo> CustomAttributes => _customAttributes.Value;

        ModuleInfo ICustomAttributeProvider.Module => ManifestModule;

        private readonly MetadataContext _metadataContext;

        private readonly AssemblyDefinition _assemblyDefinition;

        private readonly Lazy<IReadOnlyList<ModuleInfo>> _modules;

        private readonly Lazy<IReadOnlyList<DefinedType>> _exportedTypes;
        private readonly Lazy<IReadOnlyList<DefinedType>> _exportedTypeTableTypes;

        private readonly Lazy<IReadOnlyCollection<ManifestResource>> _manifestResources;

        private readonly Lazy<IReadOnlyCollection<AssemblyIdentity>> _referencedAssemblies;

        private readonly Lazy<IReadOnlyCollection<CustomAttributeInfo>> _customAttributes;

        internal AssemblyInfo(
            MetadataContext metadataContext,
            AssemblyIdentity identity,
            ModuleReader manifestModuleReader)
        {
            Identity = identity;

            _metadataContext = metadataContext;

            _assemblyDefinition = manifestModuleReader.MetadataReader.GetAssemblyDefinition();

            _modules = new Lazy<IReadOnlyList<ModuleInfo>>(ResolveModules);

            _exportedTypes = new Lazy<IReadOnlyList<DefinedType>>(ResolveExportedTypes);
            _exportedTypeTableTypes = new Lazy<IReadOnlyList<DefinedType>>(ResolveExportedTypeTableTypes);

            _manifestResources = new Lazy<IReadOnlyCollection<ManifestResource>>(ResolveManifestResources);

            _referencedAssemblies = new Lazy<IReadOnlyCollection<AssemblyIdentity>>(ResolveReferencedAssemblies);

            _customAttributes = new Lazy<IReadOnlyCollection<CustomAttributeInfo>>(ResolveCustomAttributes);

            ManifestModule = new ModuleInfo(_metadataContext, this, manifestModuleReader);
        }

        public override string ToString() => Identity.ToString();

        internal Stream ResolveFile(AssemblyFile assemblyFile)
        {
            System.Diagnostics.Debug.Assert(!assemblyFile.ContainsMetadata);

            var name = ManifestModule.MetadataReader.GetString(assemblyFile.Name);
            return _metadataContext.ResolveFile(this, name);
        }

        internal ModuleInfo ResolveModule(AssemblyFile assemblyFile)
        {
            System.Diagnostics.Debug.Assert(assemblyFile.ContainsMetadata);

            var name = ManifestModule.MetadataReader.GetString(assemblyFile.Name);
            return ResolveModule(name);
        }

        internal ModuleInfo ResolveModuleReference(ModuleReference moduleReference)
        {
            var name = ManifestModule.MetadataReader.GetString(moduleReference.Name);
            return ResolveModule(name);
        }

        internal DefinedType ResolveExportedType(string typeNamespace, string name)
        {
            foreach (var type in ExportedTypes)
            {
                if (type.Namespace == typeNamespace
                    && type.Name == name)
                {
                    return type;
                }
            }

            throw new InvalidOperationException();
        }

        internal DefinedType ResolveExportedTypeTableType(string typeNamespace, string name)
        {
            foreach (var type in _exportedTypeTableTypes.Value)
            {
                if (type.Namespace == typeNamespace
                    && type.Name == name)
                {
                    return type;
                }
            }

            throw new InvalidOperationException();
        }

        internal DefinedType ResolveType(string typeNamespace, string name)
        {
            foreach (var module in Modules)
            {
                foreach (var type in module.Types)
                {
                    if (type.Namespace == typeNamespace
                        && type.Name == name)
                    {
                        return type;
                    }
                }
            }

            return ResolveExportedTypeTableType(typeNamespace, name);
        }

        // https://gist.github.com/nguerrera/6864d2a907cb07d869be5a2afed8d764
        internal unsafe Stream ResolveResource(long offset)
        {
            var peReader = ManifestModule.PEReader;

            checked // arithmetic overflow here could cause AV
            {
                // Locate start and end of PE image in unmanaged memory.
                var block = peReader.GetEntireImage();

                System.Diagnostics.Debug.Assert(block.Pointer != null && block.Length > 0);

                byte* peImageStart = block.Pointer;
                byte* peImageEnd = peImageStart + block.Length;

                // Locate offset to resources within PE image.
                if (!peReader.PEHeaders.TryGetDirectoryOffset(
                    peReader.PEHeaders.CorHeader.ResourcesDirectory, out var offsetToResources))
                {
                    throw new InvalidDataException("Failed to get offset to resources in PE file.");
                }

                System.Diagnostics.Debug.Assert(offsetToResources > 0);

                byte* resourceStart = peImageStart + offsetToResources + offset;

                // Get the length of the the resource from the first 4 bytes.
                if (resourceStart >= peImageEnd - sizeof(int))
                {
                    throw new InvalidDataException("resource offset out of bounds.");
                }

                int resourceLength = new BlobReader(resourceStart, sizeof(int)).ReadInt32();
                resourceStart += sizeof(int);
                if (resourceLength < 0 || resourceStart >= peImageEnd - resourceLength)
                {
                    throw new InvalidDataException("resource offset or length out of bounds.");
                }

                return new UnmanagedMemoryStream(resourceStart, resourceLength);
            }
        }

        private IReadOnlyList<ModuleInfo> ResolveModules()
        {
            var modules = new List<ModuleInfo>();

            // main module
            modules.Add(ManifestModule);

            foreach (var handle in ManifestModule.MetadataReader.AssemblyFiles)
            {
                var assemblyFile = ManifestModule.MetadataReader.GetAssemblyFile(handle);

                if (assemblyFile.ContainsMetadata)
                {
                    modules.Add(_metadataContext.ResolveModule(
                        this, ManifestModule.MetadataReader.GetString(assemblyFile.Name)));
                }
            }

            return modules;
        }

        private IReadOnlyList<DefinedType> ResolveExportedTypes()
        {
            var exportedTypeHandles = ManifestModule.MetadataReader.ExportedTypes;
            var exportedTypes = new List<DefinedType>(exportedTypeHandles.Count);

            exportedTypes.AddRange(_exportedTypeTableTypes.Value);

            foreach (var type in ManifestModule.Types)
            {
                if (type.IsPublic)
                {
                    exportedTypes.Add(type);
                }
            }

            return exportedTypes;
        }

        private IReadOnlyList<DefinedType> ResolveExportedTypeTableTypes()
        {
            var exportedTypeHandles = ManifestModule.MetadataReader.ExportedTypes;
            var exportedTypes = new List<DefinedType>(exportedTypeHandles.Count);

            foreach (var exportedTypeHandle in exportedTypeHandles)
            {
                var exportedType = ManifestModule.MetadataReader.GetExportedType(exportedTypeHandle);
                var type = ResolveExportedType(exportedType);

                if (type != null)
                {
                    exportedTypes.Add(type);
                }
            }

            return exportedTypes;
        }

        private IReadOnlyCollection<ManifestResource> ResolveManifestResources()
        {
            var handles = ManifestModule.MetadataReader.ManifestResources;
            var manifestResources = new List<ManifestResource>(handles.Count);

            foreach (var handle in handles)
            {
                manifestResources.Add(new ManifestResource(this, handle));
            }

            return manifestResources;
        }

        private IReadOnlyCollection<AssemblyIdentity> ResolveReferencedAssemblies()
        {
            var handles = ManifestModule.MetadataReader.AssemblyReferences;
            var referencedAssemblies = new List<AssemblyIdentity>(handles.Count);

            foreach (var handle in handles)
            {
                referencedAssemblies.Add(
                    AssemblyIdentity.FromAssemblyReference(
                        ManifestModule.MetadataReader,
                        ManifestModule.MetadataReader.GetAssemblyReference(handle)));
            }

            return referencedAssemblies;
        }

        private IReadOnlyCollection<CustomAttributeInfo> ResolveCustomAttributes() =>
            ManifestModule.ResolveCustomAttributes(_assemblyDefinition.GetCustomAttributes());

        private DefinedType ResolveExportedType(ExportedType exportedType)
        {
            var exportedTypeNamespace = ManifestModule.MetadataReader.GetString(exportedType.Namespace);
            var exportedTypeName = ManifestModule.MetadataReader.GetString(exportedType.Name);

            var typeImplementationHandle = exportedType.Implementation;

            switch (typeImplementationHandle.Kind)
            {
                case HandleKind.AssemblyFile:

                    var assemblyFileHandle = (AssemblyFileHandle)typeImplementationHandle;
                    var assemblyFile = ManifestModule.MetadataReader.GetAssemblyFile(assemblyFileHandle);
                    var module = ResolveModule(assemblyFile);

                    if (module == null)
                    {
                        return null;
                    }

                    var rowId = exportedType.GetTypeDefinitionId();
                    var typeDefinitionHandle = MetadataTokens.TypeDefinitionHandle(rowId);

                    var type = module.ResolveTypeDefinition(typeDefinitionHandle);

                    if (type.Namespace == exportedTypeNamespace
                        && type.Name == exportedTypeName)
                    {
                        return type;
                    }

                    return module.ResolveType(exportedTypeNamespace, exportedTypeName);

                case HandleKind.ExportedType:

                    var declaringTypeHandle = (ExportedTypeHandle)typeImplementationHandle;
                    var declaringType = ManifestModule.MetadataReader.GetExportedType(declaringTypeHandle);
                    var parent = ResolveExportedType(declaringType);

                    return new ReferencedType(
                        null,
                        exportedTypeName,
                        () => (DefinedType)parent.ResolveNestedType(exportedTypeName));

                case HandleKind.AssemblyReference when exportedType.IsForwarder:

                    var assemblyReferenceHandle = (AssemblyReferenceHandle)typeImplementationHandle;
                    var assemblyReference = ManifestModule.MetadataReader.GetAssemblyReference(assemblyReferenceHandle);

                    return new ReferencedType(
                        exportedTypeNamespace,
                        exportedTypeName,
                        () =>
                        {
                            var referencedAssembly = _metadataContext.ResolveAssemblyReference(
                                ManifestModule.MetadataReader, assemblyReference);
                            return referencedAssembly.ResolveType(exportedTypeNamespace, exportedTypeName);
                        });

                default:
                    throw new BadImageFormatException();
            }
        }

        private ModuleInfo ResolveModule(string name)
        {
            foreach (var module in Modules)
            {
                if (module.Name == name)
                {
                    return module;
                }
            }

            throw new FileNotFoundException("Module file not found!", name);
        }
    }
}
