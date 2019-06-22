using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;

namespace IL2CPU.Reflection
{
    public class MethodBody
    {
        public IReadOnlyList<TypeInfo> LocalTypes => _locals.Value;
        public IReadOnlyList<ExceptionBlock> ExceptionBlocks => _exceptionBlocks.Value;

        public bool InitLocals => _methodBodyBlock.LocalVariablesInitialized;
        public int MaxStackSize => _methodBodyBlock.MaxStack;

        public ImmutableArray<byte> ILBytes => _methodBodyBlock.GetILContent();

        private readonly ModuleInfo _module;
        private readonly MethodBodyBlock _methodBodyBlock;

        private readonly GenericContext _genericContext;

        private readonly Lazy<IReadOnlyList<TypeInfo>> _locals;
        private readonly Lazy<IReadOnlyList<ExceptionBlock>> _exceptionBlocks;

        internal MethodBody(
            ModuleInfo module,
            MethodBodyBlock methodBodyBlock,
            GenericContext genericContext)
        {
            _module = module;
            _methodBodyBlock = methodBodyBlock;

            _genericContext = genericContext;

            _locals = new Lazy<IReadOnlyList<TypeInfo>>(DecodeLocalSignature);
            _exceptionBlocks = new Lazy<IReadOnlyList<ExceptionBlock>>(ResolveExceptionBlocks);
        }

        public ILReader GetILReader() => new ILReader(_module, _genericContext, this);

        internal BlobReader GetILBlobReader() => _methodBodyBlock.GetILReader();

        private IReadOnlyList<TypeInfo> DecodeLocalSignature()
        {
            var handle = _methodBodyBlock.LocalSignature;

            if (handle.IsNil)
            {
                return Array.Empty<TypeInfo>();
            }

            var localsSignature = _module.MetadataReader.GetStandaloneSignature(handle);
            return localsSignature.DecodeLocalSignature(_module.TypeProvider, _genericContext);
        }

        private IReadOnlyList<ExceptionBlock> ResolveExceptionBlocks() =>
            _methodBodyBlock.ExceptionRegions.Select(e => new ExceptionBlock(_module, e)).ToList();
    }
}
