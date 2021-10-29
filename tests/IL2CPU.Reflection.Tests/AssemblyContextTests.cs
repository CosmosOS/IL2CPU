using System.Reflection;
using Cosmos.IL2CPU;
using NUnit.Framework;

namespace IL2CPU.Reflection.Tests
{
    [TestFixture]
    public class AssemblyContextTests
    {
        [OneTimeSetUp]
        public void Setup()
        {
            TestHelpers.SetupAssemblyContext();
        }

        [Test]
        public void ShouldLoadByName()
        {
            var source = typeof(string).Assembly;
            var stringAsmblName = source.GetName().Name;
            var ctx = IsolatedAssemblyLoadContext.Default;
            var asmbl = ctx.LoadFromAssemblyName(new AssemblyName(stringAsmblName));
            Assert.AreEqual(asmbl.Location, source.Location);
        }

        [Test]
        public void ShouldCheckIdentity()
        {
            var first = new AssemblyIdentity(typeof(string).Assembly.GetName());
            Assert.AreEqual("System.Private.CoreLib", first.ToString());

            var second = new AssemblyIdentity(typeof(int).Assembly.GetName());
            Assert.AreEqual("System.Private.CoreLib", second.ToString());

            Assert.True(first.Equals((object)second));
        }
    }
}
