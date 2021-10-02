using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using static IL2CPU.Reflection.Tests.SampleObjects;

namespace IL2CPU.Reflection.Tests
{
    [TestFixture]
    public class MethodTests
    {
        [OneTimeSetUp]
        public void Setup()
        {
            TestHelpers.SetupAssemblyContext();
        }

        [TestCase(typeof(object), nameof(ToString))]
        [TestCase(typeof(object), nameof(GetType))]
        [TestCase(typeof(ValueType), nameof(ToString))]
        [TestCase(typeof(ValueType), nameof(GetType))]
        public void ShouldBeTheSame(Type rtType, string methodName)
        {
            var rtFirst = rtType.GetMethod(methodName);
            var rtSecond = FindByToken(rtType, rtFirst.MetadataToken);
            Assert.IsTrue(rtFirst.IsSame(rtSecond));
            Assert.IsTrue(rtSecond.IsSame(rtFirst));

            var loType = TypeofExtensions.Reload(rtType);
            var loFirst = loType.GetMethod(methodName);
            var loSecond = FindByToken(loType, rtFirst.MetadataToken);
            Assert.IsTrue(loFirst.IsSame(loSecond));
            Assert.IsTrue(loSecond.IsSame(loFirst));

            Assert.IsFalse(rtFirst.IsSame(loFirst));
            Assert.IsFalse(loFirst.IsSame(rtFirst));
            Assert.IsFalse(rtSecond.IsSame(loSecond));
            Assert.IsFalse(loSecond.IsSame(rtSecond));

            Assert.IsFalse(rtFirst.IsSame(null));
            Assert.IsFalse(loFirst.IsSame(null));
            Assert.IsTrue(rtFirst.IsSame(rtFirst));
            Assert.IsTrue(loFirst.IsSame(loFirst));
        }

        private static MethodInfo FindByToken(Type type, int metaToken)
        {
            return type.Assembly.GetTypes()
                .SelectMany(t => t.GetMethods())
                .FirstOrDefault(m => m.MetadataToken == metaToken);
        }

        [TestCase(typeof(object), nameof(ToString))]
        [TestCase(typeof(object), nameof(GetType))]
        [TestCase(typeof(ValueType), nameof(ToString))]
        [TestCase(typeof(ValueType), nameof(GetType))]
        [TestCase(typeof(ArrayList), nameof(ToString))]
        [TestCase(typeof(ArrayList), nameof(GetType))]
        [TestCase(typeof(ArrayList), nameof(ArrayList.Add))]
        [TestCase(typeof(List<string>), nameof(ToString))]
        [TestCase(typeof(List<string>), nameof(GetType))]
        [TestCase(typeof(List<string>), nameof(List<string>.Add))]
        [TestCase(typeof(Dictionary<string, string>), nameof(ToString))]
        [TestCase(typeof(Dictionary<string, string>), nameof(GetType))]
        [TestCase(typeof(Dictionary<string, string>), nameof(Dictionary<string, string>.Add))]
        [TestCase(typeof(Tuple<string, string, string>), nameof(ToString))]
        [TestCase(typeof(Tuple<string, string, string>), nameof(GetType))]
        [TestCase(typeof(JObject), nameof(ToString))]
        [TestCase(typeof(JObject), nameof(GetType))]
        [TestCase(typeof(JValue), nameof(ToString))]
        [TestCase(typeof(JValue), nameof(GetType))]
        [TestCase(typeof(ICloneable), nameof(ICloneable.Clone))]
        [TestCase(typeof(IEnumerator), nameof(IEnumerator.MoveNext))]
        [TestCase(typeof(List<bool>), nameof(IList.Clear))]
        [TestCase(typeof(List<bool>), nameof(IList.CopyTo))]
        [TestCase(typeof(List<bool>), nameof(IList.GetEnumerator))]
        [TestCase(typeof(List<bool>), nameof(IList.Add))]
        [TestCase(typeof(IList), nameof(IList.Clear))]
        [TestCase(typeof(IList), nameof(IList.Add))]
        [TestCase(typeof(DerivedClass), nameof(DerivedClass.ToString))]
        [TestCase(typeof(DerivedClass), nameof(DerivedClass.Equals))]
        [TestCase(typeof(DerivedClass), nameof(DerivedClass.InterfaceImpl))]
        [TestCase(typeof(DerivedClass), nameof(DerivedClass.Method1))]
        [TestCase(typeof(DerivedClass), nameof(DerivedClass.Method2))]
        [TestCase(typeof(DerivedClass), nameof(DerivedClass.Method3))]
        public void ShouldGetBaseDefinition(Type rtType, string methodName)
        {
            var rtParmMethod = rtType.GetMethods().FirstOrDefault(m => m.Name == methodName);
            var rtRealMethod = rtParmMethod?.GetBaseDefinition();
            var loParmMethod = TypeofExtensions.Reload(rtType).GetMethods().FirstOrDefault(m => m.Name == methodName);
            var loRealMethod = loParmMethod?.GetMyBaseDefinition();
            var rt = rtRealMethod?.ToFullStr();
            var lo = loRealMethod?.ToFullStr();
            Assert.AreEqual(rt, lo);
            Assert.IsNotNull(rt);
        }
    }
}
