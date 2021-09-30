using System;
using System.Reflection;

namespace IL2CPU.Reflection
{
    public static class TypeofExtensions
    {
        public static Type Reload<T>()
            => Reload(typeof(T));

        public static Type Reload(Type type)
            => Reload(type.FullName, type.Assembly.GetName(), type.Assembly.Location);

        private static Type Reload(string fullName, AssemblyName name, string path)
        {
            var ctx = IsolatedAssemblyLoadContext.Default;
            ctx.LoadOrAddByPath(path);
            var defaultLoader = ctx.GetLoader();
            var asmbl = defaultLoader.LoadFromAssemblyName(name);
            var realType = asmbl.GetType(fullName, throwOnError: true, ignoreCase: false);
            return realType;
        }
    }
}
