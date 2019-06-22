using System;
using System.Reflection.Metadata;

namespace IL2CPU.Reflection
{
    internal class MethodImpl
    {
        public MethodInfo MethodDeclaration => _methodDeclaration.Value;
        public MethodInfo MethodBody => _methodBody.Value;

        private readonly ModuleInfo _module;
        private readonly GenericContext _genericContext;

        private readonly MethodImplementationHandle _methodImplementationHandle;
        private readonly MethodImplementation _methodImplementation;

        private readonly Lazy<MethodInfo> _methodDeclaration;
        private readonly Lazy<MethodInfo> _methodBody;

        public MethodImpl(
            ModuleInfo module,
            GenericContext genericContext,
            MethodImplementationHandle methodImplementationHandle)
        {
            _module = module;
            _genericContext = genericContext;

            _methodImplementationHandle = methodImplementationHandle;
            _methodImplementation = module.MetadataReader.GetMethodImplementation(_methodImplementationHandle);

            _methodDeclaration = new Lazy<MethodInfo>(ResolveMethodDeclaration);
            _methodBody = new Lazy<MethodInfo>(ResolveMethodBody);
        }

        private MethodInfo ResolveMethodDeclaration() =>
            _module.ResolveMethodHandle(_methodImplementation.MethodDeclaration, _genericContext);

        private MethodInfo ResolveMethodBody() =>
            _module.ResolveMethodHandle(_methodImplementation.MethodBody, _genericContext);
    }
}
