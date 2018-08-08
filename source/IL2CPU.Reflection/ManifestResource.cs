using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using Metadata = System.Reflection.Metadata;

using IL2CPU.Reflection.Internal;

namespace IL2CPU.Reflection
{
    public sealed class ManifestResource : ICustomAttributeProvider
    {
        public string Name => _name.Value;

        public IReadOnlyCollection<CustomAttributeInfo> CustomAttributes => _customAttributes.Value;

        public ManifestResourceAttributes Attributes => _manifestResource.Attributes;

        #region Manifest resource attribute wrappers

        public bool IsPrivate => Attributes.HasFlag(ManifestResourceAttributes.Private);
        public bool IsPublic => Attributes.HasFlag(ManifestResourceAttributes.Public);

        #endregion

        ModuleInfo ICustomAttributeProvider.Module => _assembly.ManifestModule;

        private ModuleInfo Module => _assembly.ManifestModule;

        private readonly AssemblyInfo _assembly;

        private readonly ManifestResourceHandle _manifestResourceHandle;
        private readonly Metadata.ManifestResource _manifestResource;

        private readonly Lazy<string> _name;

        private readonly Lazy<IReadOnlyCollection<CustomAttributeInfo>> _customAttributes;

        internal ManifestResource(
            AssemblyInfo assembly,
            ManifestResourceHandle manifestResourceHandle)
        {
            _assembly = assembly;

            _manifestResourceHandle = manifestResourceHandle;
            _manifestResource = _assembly.ManifestModule.MetadataReader.GetManifestResource(_manifestResourceHandle);

            _name = new Lazy<string>(GetName);

            _customAttributes = new Lazy<IReadOnlyCollection<CustomAttributeInfo>>(ResolveCustomAttributes);
        }

        public Stream GetResourceStream()
        {
            var handle = _manifestResource.Implementation;

            if (handle.IsNil)
            {
                return _assembly.ResolveResource(_manifestResource.Offset);
            }

            switch (handle.Kind)
            {
                case HandleKind.AssemblyReference:

                    var assemblyReference = Module.MetadataReader.GetAssemblyReference((AssemblyReferenceHandle)handle);
                    var assembly = _assembly.ManifestModule.MetadataContext.ResolveAssemblyReference(
                        _assembly.ManifestModule.MetadataReader, assemblyReference);

                    return assembly.ResolveResource(_manifestResource.Offset);

                case HandleKind.AssemblyFile:

                    var assemblyFile = Module.MetadataReader.GetAssemblyFile((AssemblyFileHandle)handle);
                    return _assembly.ResolveFile(assemblyFile);

                default:
                    throw new BadImageFormatException();
            }
        }

        private string GetName() => _assembly.ManifestModule.MetadataReader.GetString(_manifestResource.Name);

        private IReadOnlyCollection<CustomAttributeInfo> ResolveCustomAttributes() =>
            _assembly.ManifestModule.ResolveCustomAttributes(_manifestResource.GetCustomAttributes());
    }
}
