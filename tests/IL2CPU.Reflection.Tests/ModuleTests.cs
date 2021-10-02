using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

using Flags = System.Reflection.BindingFlags;
using static IL2CPU.Reflection.Tests.SampleObjects;

namespace IL2CPU.Reflection.Tests
{
    [TestFixture]
    public class ModuleTests
    {
        [OneTimeSetUp]
        public void Setup()
        {
            TestHelpers.SetupAssemblyContext();
        }

        [Test]
        public void ShouldFetchJObjectType()
        {
            var rtMember = typeof(JObject);
            var loMember = TypeofExtensions.Reload<JObject>();
            CheckResolve(rtMember, loMember);
        }

        [Test]
        public void ShouldFetchTupleGenType()
        {
            var rtMember = typeof(Tuple<bool>);
            var loMember = TypeofExtensions.Reload<Tuple<bool>>();
            CheckResolve(rtMember, loMember);
        }

        [Test]
        public void ShouldFetchJsonConvertField()
        {
            var rtMember = typeof(JsonConvert).GetField(nameof(JsonConvert.True));
            var loMember = TypeofExtensions.Reload(typeof(JsonConvert));
            CheckResolve(rtMember, loMember);
        }

        [Test]
        public void ShouldFetchJsonConvertMethod()
        {
            var param = new[] { typeof(object) };
            var rtMember = typeof(JsonConvert).GetMethod(nameof(JsonConvert.SerializeObject), param);
            var loMember = TypeofExtensions.Reload(typeof(JsonConvert));
            CheckResolve(rtMember, loMember);
        }

        [Test]
        public void ShouldFetchJsonConvertGenMethod()
        {
            var param = new[] { typeof(string), typeof(JsonConverter[]) };
            var rtMember = typeof(JsonConvert).GetMethod(nameof(JsonConvert.DeserializeObject), param);
            var loMember = TypeofExtensions.Reload(typeof(JsonConvert));
            CheckResolve(rtMember, loMember);
        }

        [Test]
        public void ShouldFetchJTokenString()
        {
            var rtMember = typeof(JToken).GetMethod(nameof(JToken.Replace));
            var ilCode = ReadIL(rtMember);
            Assert.AreEqual(13, ilCode.counter);
            var stringToken = ilCode.reader.StringTokens.Single();
            var loMember = TypeofExtensions.Reload(typeof(JToken));
            var rtStr = ModuleExtensions.ResolveMyString(rtMember.Module, stringToken);
            var loStr = ModuleExtensions.ResolveMyString(loMember.Module, stringToken);
            Assert.AreEqual("The parent is missing.", rtStr);
            Assert.AreEqual("The parent is missing.", loStr);
        }

        [Test]
        public void ShouldFetchJsonTextWriterMethod()
        {
            var rtMember = typeof(JsonTextWriter).GetMethod(nameof(JsonTextWriter.Flush));
            var ilCode = ReadIL(rtMember);
            Assert.AreEqual(4, ilCode.counter);
            var methodToken = ilCode.reader.MethodTokens.Single();
            var loMember = TypeofExtensions.Reload(typeof(JsonTextWriter));
            CheckResolve(rtMember, loMember, metaToken: methodToken);
        }

        [Test]
        public void ShouldFetchJValueField()
        {
            var rtMember = typeof(JValue).GetMethod(nameof(JValue.ToString), Type.EmptyTypes);
            var ilCode = ReadIL(rtMember);
            Assert.AreEqual(10, ilCode.counter);
            var fieldToken = ilCode.reader.FieldTokens.Skip(1).FirstOrDefault();
            var loMember = TypeofExtensions.Reload(typeof(JValue));
            var rtf = rtMember.DeclaringType.GetFields(Flags.NonPublic | Flags.Instance)[0];
            CheckResolve(rtf, loMember, metaToken: fieldToken);
        }

        [Test]
        public void ShouldFetchUtilitiesType()
        {
            var ru = typeof(JObject).Module.GetType("Newtonsoft.Json.Utilities.ReflectionUtils");
            var rtMember = ru.GetMethod("GetDefaultValue");
            var ilCode = ReadIL(rtMember);
            Assert.AreEqual(153, ilCode.counter);
            var typeToken = ilCode.reader.TypeTokens.Skip(1).FirstOrDefault();
            var loMember = TypeofExtensions.Reload(typeof(JObject));
            CheckResolve(rtMember.DeclaringType, loMember, metaToken: typeToken);
        }

        [Test]
        public void ShouldFetchMethodSpec()
        {
            var rtMember = typeof(JToken).GetMethod(nameof(JToken.ToString), Type.EmptyTypes);
            var ilCode = ReadIL(rtMember);
            Assert.AreEqual(5, ilCode.counter);
            var methodToken = ilCode.reader.MethodTokens.First();
            var loMember = TypeofExtensions.Reload(typeof(JToken));
            CheckResolve(rtMember, loMember, metaToken: methodToken);
        }

        [Test]
        public void ShouldFetchTypeSpec()
        {
            var ru = typeof(JObject).Module.GetType("Newtonsoft.Json.Bson.BsonObject");
            var rtMember = ru.GetMethod("GetEnumerator");
            var ilCode = ReadIL(rtMember);
            Assert.AreEqual(5, ilCode.counter);
            var typeToken = ilCode.reader.TypeTokens.Single();
            var loMember = TypeofExtensions.Reload(typeof(JObject));
            CheckResolve(rtMember.DeclaringType, loMember, metaToken: typeToken);
        }

        [Test]
        public void ShouldFetchPrimitives()
        {
            var rtMember = typeof(WeirdTyping).GetMethod(nameof(WeirdTyping.RenderPrimitives));
            var ilCode = ReadIL(rtMember);
            Assert.AreEqual(227, ilCode.counter);
            var loMember = TypeofExtensions.Reload<WeirdTyping>();

            var typeTokens = ilCode.reader.TypeTokens;
            Assert.AreEqual(18, typeTokens.Count);
            for (var i = 0; i < typeTokens.Count; i++)
            {
                var typeToken = typeTokens[i];
                CheckResolve(rtMember.DeclaringType, loMember, metaToken: typeToken);
            }

            var methTokens = ilCode.reader.MethodTokens;
            Assert.AreEqual(23, methTokens.Count);
            for (var i = 0; i < methTokens.Count; i++)
            {
                var methToken = methTokens[i];
                CheckResolve(rtMember, loMember, metaToken: methToken);
            }
        }

        [Test]
        public void ShouldFetchEdge()
        {
            var rtMember = typeof(WeirdTyping).GetMethod(nameof(WeirdTyping.DoEdgeCases));
            var ilCode = ReadIL(rtMember);
            Assert.AreEqual(217, ilCode.counter);
            var loMember = TypeofExtensions.Reload<WeirdTyping>();

            var fieldTokens = ilCode.reader.FieldTokens;
            Assert.AreEqual(8, fieldTokens.Count);
            for (var i = 0; i < fieldTokens.Count; i++)
            {
                var fieldToken = fieldTokens[i];
                CheckResolve(rtMember.DeclaringType.GetField(nameof(WeirdTyping.Marker)), loMember, metaToken: fieldToken);
            }

            var methTokens = ilCode.reader.MethodTokens;
            Assert.AreEqual(24, methTokens.Count);
            for (var i = 0; i < methTokens.Count; i++)
            {
                var methToken = methTokens[i];
                CheckResolve(rtMember, loMember, metaToken: methToken);
            }

            var typeTokens = ilCode.reader.TypeTokens;
            Assert.AreEqual(4, typeTokens.Count);
            for (var i = 0; i < typeTokens.Count; i++)
            {
                var typeToken = typeTokens[i];
                CheckResolve(rtMember.DeclaringType, loMember, metaToken: typeToken);
            }
        }

        [Test]
        public void ShouldFetchComplex()
        {
            var rtMember = typeof(WeirdTyping).GetMethod(nameof(WeirdTyping.RenderComplex));
            var ilCode = ReadIL(rtMember);
            Assert.AreEqual(200, ilCode.counter);
            var loMember = TypeofExtensions.Reload<WeirdTyping>();

            var typeTokens = ilCode.reader.TypeTokens;
            Assert.AreEqual(14, typeTokens.Count);
            for (var i = 0; i < typeTokens.Count; i++)
            {
                var typeToken = typeTokens[i];
                CheckResolve(rtMember.DeclaringType, loMember, metaToken: typeToken);
            }

            var methTokens = ilCode.reader.MethodTokens;
            Assert.AreEqual(19, methTokens.Count);
            for (var i = 0; i < methTokens.Count; i++)
            {
                var methToken = methTokens[i];
                CheckResolve(rtMember, loMember, metaToken: methToken);
            }
        }

        private static (int counter, WackyILReader reader) ReadIL(MethodInfo method)
        {
            var counter = 0;
            var reader = new WackyILReader(method);
            while (reader.Read())
                counter++;
            return (counter, reader);
        }

        private static void CheckResolve(MemberInfo rtMember, MemberInfo loMember,
            Type[] rtTypeArgs = null, Type[] rtMethArgs = null,
            Type[] loTypeArgs = null, Type[] loMethArgs = null,
            int? metaToken = null)
        {
            var token = metaToken ?? rtMember.MetadataToken;
            var isType = rtMember is Type;
            var isField = rtMember is FieldInfo;
            var isMethod = rtMember is MethodInfo;

            var rtModule = rtMember.Module;
            var rtResolved = isType ? rtModule.ResolveType(token, rtTypeArgs, rtMethArgs)
                : isField ? (MemberInfo)rtModule.ResolveField(token, rtTypeArgs, rtMethArgs)
                : isMethod ? rtModule.ResolveMethod(token, rtTypeArgs, rtMethArgs)
                : throw new InvalidOperationException(rtMember.ToString());

            var loModule = loMember.Module;
            var loResolved = isType ? loModule.ResolveMyType(token, loTypeArgs, loMethArgs)
                : isField ? (MemberInfo)loModule.ResolveMyField(token, loTypeArgs, loMethArgs)
                : isMethod ? loModule.ResolveMyMethod(token, loTypeArgs, loMethArgs)
                : throw new InvalidOperationException(loMember.ToString());

            if (isType)
                Assert.AreEqual(((Type)rtResolved).FullName, ((Type)loResolved).FullName);
            else if (isMethod)
                Assert.AreEqual(((MethodBase)rtResolved).ToFullStr(true), ((MethodBase)loResolved).ToFullStr(true));
            else
                Assert.AreEqual(((FieldInfo)rtResolved).ToFullStr(true), ((FieldInfo)loResolved).ToFullStr(true));
        }
    }
}
