using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using static IL2CPU.Reflection.Tests.SampleObjects;

namespace IL2CPU.Reflection.Tests
{
    [TestFixture]
    public class TypeExtTests
    {
        [OneTimeSetUp]
        public void Setup()
        {
            TestHelpers.SetupAssemblyContext();
        }

        [TestCase("System.String", typeof(string))]
        [TestCase("System.Collections.Generic.Stack`1", typeof(Stack<string>))]
        [TestCase("System.Collections.Generic.IReadOnlyCollection`1", typeof(IReadOnlyCollection<string>))]
        [TestCase("System.Collections.Generic.HashSet`1", typeof(HashSet<string>))]
        [TestCase("System.Collections.Generic.ICollection`1", typeof(ICollection<string>))]
        [TestCase("System.Tuple`3", typeof(Tuple<bool, char, long>))]
        [TestCase("System.Runtime.CompilerServices.ITuple", typeof(ITuple))]
        [TestCase("System.Collections.Generic.SortedDictionary`2", typeof(SortedDictionary<string, int>))]
        [TestCase("System.Collections.Generic.IDictionary`2", typeof(IDictionary<string, int>))]
        [TestCase("System.IO.MemoryStream", typeof(MemoryStream))]
        [TestCase("System.IDisposable", typeof(IDisposable))]
        [TestCase("System.Collections.Generic.List`1+Enumerator", typeof(List<string>.Enumerator))]
        public void ShouldCheckNames(string fullName, Type type)
        {
            Assert.AreEqual(fullName, TypeExtensions.GetNestedName(type));
        }

        [TestCase(typeof(string))]
        [TestCase(typeof(List<string>))]
        [TestCase(typeof(Dictionary<string, long>))]
        [TestCase(typeof(Tuple<string, long, bool>))]
        [TestCase(typeof(Tuple<string, long, bool, HashSet<Stack<DateTime>>>))]
        public void ShouldReplaceLoad(Type type)
        {
            var fullName = type.FullName;
            var replaced = TypeExtensions.ReplaceLoad(type);
            Assert.AreEqual(fullName, replaced.FullName);
            Assert.AreEqual("RuntimeModule", type.Module.GetType().Name);
            Assert.AreEqual("EcmaModule", replaced.Module.GetType().Name);
        }

        [Test]
        public void ShouldGetInterfaceMapStack()
        {
            CheckGetInterfaceMap<Stack<string>, IReadOnlyCollection<string>>();
        }

        [Test]
        public void ShouldGetInterfaceMapHash()
        {
            CheckGetInterfaceMap<HashSet<string>, ICollection<string>>();
        }

        [Test]
        public void ShouldGetInterfaceMapTuple()
        {
            CheckGetInterfaceMap<Tuple<bool, char, long>, ITuple>();
        }

        [Test]
        public void ShouldGetInterfaceMapDict()
        {
            CheckGetInterfaceMap<SortedDictionary<string, int>, IDictionary<string, int>>();
        }

        [Test]
        public void ShouldGetInterfaceMapEnumerator()
        {
            CheckGetInterfaceMap<ArrayEnumerator<object>, IEnumerator>();
        }

        [Test]
        public void ShouldGetInterfaceMapGeneric()
        {
            CheckGetInterfaceMap<ComplexEnumerator, IDisposable>();
            CheckGetInterfaceMap<ComplexEnumerator, ICloneable>();
            CheckGetInterfaceMap<ComplexEnumerator, IEnumerable>();
            CheckGetInterfaceMap<ComplexEnumerator, IEnumerable<ulong>>();
            CheckGetInterfaceMap<ComplexEnumerator, IEnumerable<string>>();
            CheckGetInterfaceMap<ComplexEnumerator, IEnumerable<bool>>();
            CheckGetInterfaceMap<ComplexEnumerator, IEnumerable<bool?>>();
            CheckGetInterfaceMap<ComplexEnumerator, IEnumerable<Dictionary<string, bool>>>();
            CheckGetInterfaceMap<ComplexEnumerator, IEnumerable<Tuple<bool, HashSet<List<string>>, float>>>();
            CheckGetInterfaceMap<ComplexEnumerator, IEnumerable<Tuple<bool, HashSet<List<string>>, long>>>();
            CheckGetInterfaceMap<ComplexEnumerator, IEnumerable<Tuple<bool, HashSet<IList<string>>, long>>>();
            CheckGetInterfaceMap<ComplexEnumerator, IEnumerable<Tuple<bool, HashSet<List<int>>, long>>>();
        }

        [Test]
        public void ShouldGetInterfaceMapMemory()
        {
            CheckGetInterfaceMap<MemoryStream, IDisposable>();
        }

        private void CheckGetInterfaceMap<TImpl, TIntf>()
        {
            var rtRType = typeof(TImpl);
            var rtIType = typeof(TIntf);
            var loRType = TypeofExtensions.Reload<TImpl>();
            var loIType = TypeofExtensions.Reload<TIntf>();
            var rtMap = Try(() => rtRType.GetInterfaceMap(rtIType));
            var loMap = Try(() => loRType.FetchInterfaceMap(loIType));
            var rtRMethods = rtMap.TargetMethods?.Select(t => t.ToFullStr()).ToArray();
            var rtIMethods = rtMap.InterfaceMethods?.Select(t => t.ToFullStr()).ToArray();
            var loRMethods = loMap.TargetMethods?.Select(t => t.ToFullStr()).ToArray();
            var loIMethods = loMap.InterfaceMethods?.Select(t => t.ToFullStr()).ToArray();
            var aDiff = rtRMethods?.Except(loRMethods ?? Array.Empty<string>());
            var bDiff = loRMethods?.Except(rtRMethods ?? Array.Empty<string>());
            var debug = String.Join(Environment.NewLine, aDiff ?? Array.Empty<string>()) +
                        Environment.NewLine + Environment.NewLine +
                          String.Join(Environment.NewLine, bDiff ?? Array.Empty<string>()) +
                        Environment.NewLine + Environment.NewLine;
            Assert.AreEqual(rtRMethods, loRMethods, debug);
            Assert.AreEqual(rtIMethods, loIMethods);
            Assert.AreEqual(rtMap.TargetType?.FullName, loMap.TargetType?.FullName);
            Assert.AreEqual(rtMap.TargetMethods?.Length, loMap.TargetMethods?.Length);
            Assert.AreEqual(rtMap.InterfaceType?.FullName, loMap.InterfaceType?.FullName);
            Assert.AreEqual(rtMap.InterfaceMethods?.Length, loMap.InterfaceMethods?.Length);
        }

        private static InterfaceMapping Try(Func<InterfaceMapping> func)
        {
            try
            {
                return func();
            }
            catch (ArgumentException)
            {
                return default;
            }
        }
    }
}
