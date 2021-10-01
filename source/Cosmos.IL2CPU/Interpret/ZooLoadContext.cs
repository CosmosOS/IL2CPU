using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Cosmos.IL2CPU.Interpret
{
    internal class ZooLoadContext : AssemblyLoadContext
    {
        protected override Assembly Load(AssemblyName assemblyName)
        {
            var name = assemblyName.ToString()
                .Split(new[] { ", Version=" }, StringSplitOptions.None)
                .First();
            var newName = new AssemblyName(name);
            var dll = Assembly.Load(newName);
            return dll;
        }
    }
}
