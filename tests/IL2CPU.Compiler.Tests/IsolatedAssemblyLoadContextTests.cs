using System;
using System.Collections.Generic;

using NUnit.Framework;

using Cosmos.IL2CPU;
using IL2CPU.Debug.Symbols;

namespace IL2CPU.Compiler.Tests
{
    [TestFixture(TestOf = typeof(IsolatedAssemblyLoadContext))]
    public class IsolatedAssemblyLoadContextTests
    {
        [Test]
        public void LoadFromAssemblyName_ForTheSameAssembly_ReturnsDifferentAssemblyInstance()
        {
            var assembly = typeof(IsolatedAssemblyLoadContext).Assembly;

            var assemblies = new List<string>()
            {
                assembly.Location,
                typeof(AssemblyFile).Assembly.Location
            };

            var assemblyLoadContext = new IsolatedAssemblyLoadContext(assemblies);
            var loadedAssembly = assemblyLoadContext.LoadFromAssemblyName(assembly.GetName());

            Assert.AreNotEqual(assembly, loadedAssembly);
        }

        [Test]
        public void Constructor_WhenMoreThanOneAssemblyWithTheSameIdentityIsAdded_ThrowsNotSupportedException()
        {
            var assemblies = new List<string>()
            {
                typeof(IsolatedAssemblyLoadContext).Assembly.Location,
                typeof(AssemblyFile).Assembly.Location,
                typeof(DebugInfo).Assembly.Location
            };

            Assert.Throws<NotSupportedException>(() => new IsolatedAssemblyLoadContext(assemblies));
        }
    }
}
