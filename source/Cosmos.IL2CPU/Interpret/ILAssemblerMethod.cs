using System;
using System.Linq;
using System.Reflection;
using XSharp.Assembler;

namespace Cosmos.IL2CPU.Extensions
{
    internal class ILAssemblerMethod : AssemblerMethod
    {
        private readonly MethodInfo _assemble;

        public ILAssemblerMethod(Type type)
        {
            if (type.BaseType?.FullName != typeof(AssemblerMethod).FullName)
            {
                throw new InvalidOperationException("No correct inheritance: " + type);
            }
            var constructor = type.GetConstructors().Single();
            var size = constructor.GetMethodBody()?.GetILAsByteArray().Length;
            if (size > 8)
            {
                throw new InvalidOperationException("Not a default constructor: " + type);
            }
            var assemble = type.GetMethod(nameof(AssemblerMethod.AssembleNew));
            if (assemble == null)
            {
                throw new InvalidOperationException("Assemble not found: " + type);
            }
            _assemble = assemble;
        }

        public override void AssembleNew(Assembler assembler, object method)
        {
            _assemble.ReInvoke(this, new[] {assembler, method});
        }
    }
}
