using System;
using System.Reflection.Metadata;

namespace IL2CPU.Reflection
{
    public class ExceptionBlock
    {
        public ExceptionBlockKind Kind => (ExceptionBlockKind)_exceptionRegion.Kind;

        public int TryOffset => _exceptionRegion.TryOffset;
        public int TryLength => _exceptionRegion.TryLength;

        public int HandlerOffset => _exceptionRegion.HandlerOffset;
        public int HandlerLength => _exceptionRegion.HandlerLength;

        public int FilterOffset => _exceptionRegion.FilterOffset;

        public TypeInfo CatchType => _catchType.Value;

        private readonly ModuleInfo _module;
        private readonly ExceptionRegion _exceptionRegion;

        private readonly Lazy<TypeInfo> _catchType;

        internal ExceptionBlock(
            ModuleInfo module,
            ExceptionRegion exceptionRegion)
        {
            _module = module;
            _exceptionRegion = exceptionRegion;

            _catchType = new Lazy<TypeInfo>(ResolveCatchType);
        }

        private TypeInfo ResolveCatchType()
        {
            var catchType = _exceptionRegion.CatchType;

            if (catchType.IsNil)
            {
                return null;
            }

            switch(catchType.Kind)
            {
                case HandleKind.TypeDefinition:
                    return _module.ResolveTypeDefinition((TypeDefinitionHandle)catchType);
                case HandleKind.TypeReference:
                    return _module.ResolveTypeReference((TypeReferenceHandle)catchType);
                default:
                    throw new BadImageFormatException();
            }
        }
    }
}
