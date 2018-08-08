using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace IL2CPU.Reflection.Internal
{
    internal class ResolvedFieldDefinition : ResolvedDefinitionBase
    {
        public override ModuleInfo Module { get; }

        public int MetadataToken => Module.MetadataReader.GetToken(_fieldDefinitionHandle);

        public string Name { get; }

        public object DefaultValue => _defaultValue.Value;
        public int Offset { get; }

        public FieldAttributes Attributes => _fieldDefinition.Attributes;

        protected override CustomAttributeHandleCollection CustomAttributeHandles =>
            _fieldDefinition.GetCustomAttributes();

        private readonly FieldDefinitionHandle _fieldDefinitionHandle;
        private readonly FieldDefinition _fieldDefinition;

        private readonly Lazy<object> _defaultValue;

        public ResolvedFieldDefinition(
            ModuleInfo module,
            FieldDefinitionHandle fieldDefinitionHandle)
        {
            Module = module;

            _fieldDefinitionHandle = fieldDefinitionHandle;
            _fieldDefinition = Module.MetadataReader.GetFieldDefinition(_fieldDefinitionHandle);

            Name = Module.MetadataReader.GetString(_fieldDefinition.Name);

            _defaultValue = new Lazy<object>(ResolveDefaultValue);
            Offset = _fieldDefinition.GetOffset();
        }

        internal TypeInfo DecodeSignature(GenericContext genericContext) =>
            _fieldDefinition.DecodeSignature(Module.TypeProvider, genericContext);

        internal byte[] GetDefaultValueBytes()
        {
            if (!Attributes.HasFlag(FieldAttributes.HasDefault))
            {
                throw new InvalidOperationException("Field doesn't have a default value!");
            }

            var defaultValue = Module.MetadataReader.GetConstant(_fieldDefinition.GetDefaultValue());
            return Module.MetadataReader.GetBlobBytes(defaultValue.Value);
        }

        internal void GetRvaBytes(byte[] buffer)
        {
            if (!Attributes.HasFlag(FieldAttributes.HasFieldRVA))
            {
                throw new InvalidOperationException("Field doesn't have RVA!");
            }

            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (buffer.Length == 0)
            {
                return;
            }

            var rva = _fieldDefinition.GetRelativeVirtualAddress();
            var memoryBlock = Module.PEReader.GetSectionData(rva);

            memoryBlock.GetContent().CopyTo(0, buffer, 0, buffer.Length);
        }

        protected override TypeDefinitionHandle GetDeclaringTypeHandle() => _fieldDefinition.GetDeclaringType();

        private object ResolveDefaultValue()
        {
            if (!Attributes.HasFlag(FieldAttributes.HasDefault))
            {
                return null;
            }

            var defaultValue = Module.MetadataReader.GetConstant(_fieldDefinition.GetDefaultValue());
            return defaultValue.GetConstantValue(Module);
        }
    }
}
