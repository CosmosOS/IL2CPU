using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using XSharp.Assembler;

namespace Cosmos.IL2CPU.Interpret
{
    internal class ProxyAssemblerMethod : AssemblerMethod
    {
        private readonly ProxyLoadContext _context;
        private readonly object _instance;

        public ProxyAssemblerMethod(ProxyLoadContext ctx, Type type)
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

        public static void DoInline(ProxyLoadContext ctx, MethodBase method, object[] args)
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
            var found = types.First(t => t.MetadataToken == type.MetadataToken);
            return found;
        }

        private static MethodBase FindMethod(AssemblyLoadContext ctx, MethodBase method)
        {
            var found = FindType(ctx, method.DeclaringType);
            var methods = found.GetMethods();
            var constr = found.GetConstructors();
            var real = methods
                .Concat<MethodBase>(constr)
                .First(m => m.MetadataToken == method.MetadataToken);
            return real;
        }
    }
}
