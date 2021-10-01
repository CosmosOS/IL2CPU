using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Cosmos.IL2CPU.Interpret;
using XSharp.Assembler;

namespace Cosmos.IL2CPU.Extensions
{
    internal class ZooAssemblerMethod : AssemblerMethod
    {
        private readonly ZooLoadContext _context;
        private readonly object _instance;

        public ZooAssemblerMethod(ZooLoadContext ctx, Type type)
        {
            _context = ctx;
            var found = FindType(ctx, type);
            _instance = Activator.CreateInstance(found);
        }

        public override void AssembleNew(Assembler assembler, object methodInfo)
        {
            var method = _instance.GetType().GetMethod(nameof(AssembleNew));
            if (methodInfo is _MethodInfo oldMethod)
            {
                var real = FindMethod(_context, oldMethod.MethodBase);
                oldMethod.MethodBase = real;
            }
            method.Invoke(_instance, new[] { assembler, methodInfo });
        }

        public static void DoInline(ZooLoadContext ctx, MethodBase method, object[] args)
        {
            var real = FindMethod(ctx, method);
            real.Invoke(null, args);
        }

        private static string GetCodeLocation(Type typeInfo)
            => typeInfo.Assembly.Location;

        private static Type FindType(AssemblyLoadContext ctx, Type type)
        {
            var codeLoc = GetCodeLocation(type);
            var dll = ctx.LoadFromAssemblyPath(codeLoc);
            var types = dll.GetTypes();
            var found = types.FirstOrDefault(t => t.MetadataToken == type.MetadataToken);
            return found;
        }

        private static MethodBase FindMethod(AssemblyLoadContext ctx, MethodBase method)
        {
            var found = FindType(ctx, method.DeclaringType);
            var methods = found.GetMethods();
            var real = methods.FirstOrDefault(m => m.MetadataToken == method.MetadataToken);
            return real;
        }
    }
}
