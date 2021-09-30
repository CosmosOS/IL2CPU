using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NUnit.Framework;
using static IL2CPU.Reflection.Tests.SampleObjects;

namespace IL2CPU.Reflection.Tests
{
    [TestFixture]
    public class AttributeTests
    {
        [OneTimeSetUp]
        public void Setup()
        {
            TestHelpers.SetupAssemblyContext();
        }

        [Test]
        public void ShouldFetchNoInherited()
        {
            const bool inherit = false;
            var rtType = typeof(SampleObject);
            var loType = TypeofExtensions.Reload<SampleObject>();
            var fo = Checks<FieldOffsetAttribute>(rtType, loType, a => a.Value + "", inherit);
            Assert.AreEqual(0, fo.Length);
            var nt = Checks<NameTag>(rtType, loType, a => a.Name, inherit);
            Assert.AreEqual(nameof(SampleObject), nt.Single());
            var st = Checks<SingleTextTag>(rtType, loType, a => a.Text, inherit);
            Assert.AreEqual(0, st.Length);
            var mt = Checks<MultiTextTag>(rtType, loType, a => a.Text, inherit);
            Assert.AreEqual(0, mt.Length);
        }

        [Test]
        public void ShouldFetchDefaultInherited()
        {
            var rtType = typeof(SampleObject);
            var loType = TypeofExtensions.Reload<SampleObject>();
            var fo = Checks<FieldOffsetAttribute>(rtType, loType, a => a.Value + "");
            Assert.AreEqual(0, fo.Length);
            var nt = Checks<NameTag>(rtType, loType, a => a.Name);
            Assert.AreEqual(nameof(SampleObject), nt.Single());
            var st = Checks<SingleTextTag>(rtType, loType, a => a.Text);
            Assert.AreEqual(nameof(SampleParent), st.Single());
            var mt = Checks<MultiTextTag>(rtType, loType, a => a.Text);
            Assert.AreEqual("SampleParent SampleBase SampleBase", String.Join(" ", mt));
        }

        [Test]
        public void ShouldFetchAllInherited()
        {
            const bool inherit = true;
            var rtType = typeof(SampleObject);
            var loType = TypeofExtensions.Reload<SampleObject>();
            var fo = Checks<FieldOffsetAttribute>(rtType, loType, a => a.Value + "", inherit);
            Assert.AreEqual(0, fo.Length);
            var nt = Checks<NameTag>(rtType, loType, a => a.Name, inherit);
            Assert.AreEqual(nameof(SampleObject), nt.Single());
            var st = Checks<SingleTextTag>(rtType, loType, a => a.Text, inherit);
            Assert.AreEqual(nameof(SampleParent), st.Single());
            var mt = Checks<MultiTextTag>(rtType, loType, a => a.Text, inherit);
            Assert.AreEqual("SampleParent SampleBase SampleBase", String.Join(" ", mt));
        }

        [Test]
        public void ShouldFetchAbstractFieldOffsets()
        {
            var rtType = typeof(ArrayImpl);
            var loType = TypeofExtensions.Reload<ArrayImpl>();
            var rtField = rtType.GetField(nameof(ArrayImpl.IntVal));
            var loField = loType.GetField(nameof(ArrayImpl.IntVal));
            var f = Checks<StoreOffset>(rtField, loField, a => a.Value + "");
            Assert.AreEqual("5", String.Join(" ", f));
        }

        [Test]
        public void ShouldFetchAbstractMethodOffsets()
        {
            var rtType = typeof(ArrayImpl);
            var loType = TypeofExtensions.Reload<ArrayImpl>();
            var rtMethod = rtType.GetMethod(nameof(ArrayImpl.GetUpperBound));
            var loMethod = loType.GetMethod(nameof(ArrayImpl.GetUpperBound));

            var m = Checks<MethodOffset>(rtMethod, loMethod, a => a.Value + "");
            Assert.AreEqual("1", String.Join(" ", m));

            var rtParm = rtMethod.GetParameters().Single();
            var loParm = loMethod.GetParameters().Single();
            var p = Checks<ParamOffset>(rtParm, loParm, a => a.Value + "");
            Assert.AreEqual("3", String.Join(" ", p));
        }

        [Test]
        public void ShouldFetchOverrideFieldOffsets()
        {
            var rtType = typeof(BetterArrayImpl);
            var loType = TypeofExtensions.Reload<BetterArrayImpl>();
            var rtField = rtType.GetField(nameof(BetterArrayImpl.IntVal));
            var loField = loType.GetField(nameof(BetterArrayImpl.IntVal));
            var f = Checks<StoreOffset>(rtField, loField, a => a.Value + "");
            Assert.AreEqual("6", String.Join(" ", f));
        }

        [Test]
        public void ShouldFetchOverrideMethodOffsets()
        {
            var rtType = typeof(BetterArrayImpl);
            var loType = TypeofExtensions.Reload<BetterArrayImpl>();
            var rtMethod = rtType.GetMethod(nameof(BetterArrayImpl.GetUpperBound));
            var loMethod = loType.GetMethod(nameof(BetterArrayImpl.GetUpperBound));

            var m = Checks<MethodOffset>(rtMethod, loMethod, a => a.Value + "");
            Assert.AreEqual("2 1", String.Join(" ", m));

            var rtParm = rtMethod.GetParameters().Single();
            var loParm = loMethod.GetParameters().Single();
            var p = Checks<ParamOffset>(rtParm, loParm, a => a.Value + "");
            Assert.AreEqual("4 3", String.Join(" ", p));
        }

        [Test]
        public void ShouldFetchTypeForwardedFrom()
        {
            var rtType = typeof(string);
            var loType = TypeofExtensions.Reload<string>();
            Check<TypeForwardedFromAttribute>(rtType, loType, a => a.AssemblyFullName);
        }

        [Test]
        public void ShouldFetchClassInterface()
        {
            var rtType = typeof(object);
            var loType = TypeofExtensions.Reload<object>();
            Check<ClassInterfaceAttribute>(rtType, loType, a => a.Value.ToString());
        }

        [Test]
        public void ShouldFetchNameTag()
        {
            var rtType = typeof(SampleObject);
            var loType = TypeofExtensions.Reload<SampleObject>();
            Check<NameTag>(rtType, loType, a => a.Name);
        }

        [Test]
        public void ShouldFetchFatherTag()
        {
            var rtType = typeof(SampleObject);
            var loType = TypeofExtensions.Reload<SampleObject>();
            Check<FatherTag>(rtType, loType, a => a?.Last);

            rtType = typeof(SampleParent);
            loType = TypeofExtensions.Reload<SampleParent>();
            Check<FatherTag>(rtType, loType, a => a.Last);
        }

        [Test]
        public void ShouldFetchAttributeUsage()
        {
            var rtType = typeof(SerializableAttribute);
            var loType = TypeofExtensions.Reload<SerializableAttribute>();
            Check<AttributeUsageAttribute>(rtType, loType,
                a => a.ValidOn + " " + a.AllowMultiple + " " + a.Inherited);
        }

        private void Check<T>(ICustomAttributeProvider rtType, ICustomAttributeProvider loType,
            Func<T, string> debug, bool? inherit = null) where T : Attribute
        {
            T rtAttr;
            T loAttr;
            if (inherit == null)
            {
                rtAttr = rtType is ParameterInfo p
                    ? p.GetCustomAttribute<T>()
                    : ((MemberInfo)rtType).GetCustomAttribute<T>();
                loAttr = loType.FetchCustomAttribute<T>();
            }
            else
            {
                rtAttr = rtType is ParameterInfo p
                    ? p.GetCustomAttribute<T>(inherit.Value)
                    : ((MemberInfo)rtType).GetCustomAttribute<T>(inherit.Value);
                loAttr = loType.FetchCustomAttribute<T>(inherit.Value);
            }
            Assert.AreEqual(rtType.ToString(), loType.ToString());
            Assert.AreEqual(rtAttr?.GetType().FullName, loAttr?.GetType().FullName);
            Assert.AreEqual(debug(rtAttr), debug(loAttr));
        }

        private string[] Checks<T>(ICustomAttributeProvider rtType, ICustomAttributeProvider loType,
            Func<T, string> debug, bool? inherit = null) where T : Attribute
        {
            T[] rtAttrs;
            T[] loAttrs;
            if (inherit == null)
            {
                rtAttrs = (rtType is ParameterInfo p
                    ? p.GetCustomAttributes<T>()
                    : ((MemberInfo)rtType).GetCustomAttributes<T>()).ToArray();
                loAttrs = loType.FetchCustomAttributes<T>().ToArray();
            }
            else
            {
                rtAttrs = (rtType is ParameterInfo p
                    ? p.GetCustomAttributes<T>(inherit.Value)
                    : ((MemberInfo)rtType).GetCustomAttributes<T>(inherit.Value)).ToArray();
                loAttrs = loType.FetchCustomAttributes<T>(inherit.Value).ToArray();
            }
            Assert.AreEqual(rtAttrs.Length, loAttrs.Length);
            var values = new string[rtAttrs.Length];
            for (var i = 0; i < rtAttrs.Length; i++)
            {
                var rtAttr = rtAttrs[i];
                var loAttr = loAttrs[i];
                Assert.True(loType.ToString().Contains(rtType.ToString()));
                Assert.AreEqual(rtAttr.GetType().FullName, loAttr.GetType().FullName);
                var rtVal = debug(rtAttr);
                var loVal = debug(loAttr);
                Assert.AreEqual(rtVal, loVal);
                values[i] = rtVal;
            }
            return values;
        }
    }
}
