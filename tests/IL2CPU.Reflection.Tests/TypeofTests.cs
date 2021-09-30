using System;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace IL2CPU.Reflection.Tests
{
    [TestFixture]
    public class TypeofTests
    {
        [OneTimeSetUp]
        public void Setup()
        {
            TestHelpers.SetupAssemblyContext();
        }

        [Test]
        public void ShouldReloadString()
        {
            var runtimeString = typeof(string);
            var loadedString = TypeofExtensions.Reload<string>();
            CheckTypes(runtimeString, loadedString);
        }

        [Test]
        public void ShouldReloadJObject()
        {
            var runtimeString = typeof(JObject);
            var loadedString = TypeofExtensions.Reload<JObject>();
            CheckTypes(runtimeString, loadedString);
        }

        private static void CheckTypes(Type runtimeString, Type loadedString)
        {
            Assert.False(runtimeString == loadedString);
            Assert.AreEqual(runtimeString.FullName, loadedString.FullName);
            Assert.AreEqual(runtimeString.Assembly.Location, loadedString.Assembly.Location);
        }
    }
}
