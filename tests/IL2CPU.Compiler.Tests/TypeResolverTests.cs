using System;
using System.Collections.Generic;

using NUnit.Framework;

using Cosmos.IL2CPU;

namespace IL2CPU.Compiler.Tests
{
    [TestFixture(TestOf = typeof(TypeResolver))]
    public class TypeResolverTests
    {
        [Test]
        public void ResolveType_ForTypeName_ThrowsNullReferenceException()
        {
            var assemblyLoadContext = GetAssemblyLoadContext();
            var typeResolver = new TypeResolver(assemblyLoadContext);

            Assert.Throws<NullReferenceException>(() => typeResolver.ResolveType("TypeResolverTests"));
        }

        [Test]
        public void ResolveType_ForFullTypeNameWithoutAssemblyPublicKeyToken_WorksForStrongNameSignedAssembly()
        {
            var assemblyLoadContext = GetAssemblyLoadContext();
            var typeResolver = new TypeResolver(assemblyLoadContext);

            // TODO: test assembly which is not loaded in the Default AssemblyLoadContext
            Assert.That(typeResolver.ResolveType("IL2CPU.Compiler.Tests.TypeResolverTests, IL2CPU.Compiler.Tests"), Is.Not.Null);
        }

        private IsolatedAssemblyLoadContext GetAssemblyLoadContext() => new IsolatedAssemblyLoadContext(GetAssemblies());

        private IEnumerable<string> GetAssemblies()
        {
            yield return typeof(TypeResolverTests).Assembly.Location;
        }
    }
}
