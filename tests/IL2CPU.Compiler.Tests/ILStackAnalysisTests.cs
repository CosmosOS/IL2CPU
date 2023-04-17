using System;
using System.IO;
using System.Text;
using System.Threading;
using Cosmos.IL2CPU;
using Cosmos.IL2CPU.MethodAnalysis;
using NUnit.Framework;
using System.Reflection;
using System.Collections;
using Cosmos.IL2CPU.CIL;

namespace IL2CPU.Compiler.Tests
{
    class FakeDisposable : IDisposable
    {
        public void Dispose() => throw new NotImplementedException();
        public void Connect(int x) => throw new NotImplementedException();
        internal object NonBlockingReceive(ref object endpoint) => throw new NotImplementedException();
    }
    public class ExampleMethods
    {
        public void TestSimpleException()
        {
            try
            {
                throw new Exception("throw new Exception()");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Caught exception.");
            }
        }

        public void FakeOpCode()
        {
            using (var xClient = new FakeDisposable())
            {
                xClient.Connect(80);
                var endpoint = new object();
                while (true)
                {
                    var data = xClient.NonBlockingReceive(ref endpoint);
                }
            }
        }
           
    }

    public class VoidTextWriter : TextWriter
    {
        public override void Write(string value) { }
        public override void Flush() { }
        public override Encoding Encoding => throw new NotImplementedException();
    }

    [TestFixture(TestOf = typeof(AppAssembler))]
    public class ILStackAnalysisTests
    {
        [Test]
        [TestCase(typeof(string), "Clone", 1, null)]
        [TestCase(typeof(string), "IndexOf", 9, new[] { typeof(string), typeof(int), typeof(int), typeof(StringComparison) })]
        [TestCase(typeof(CancellationTokenSource), "ExecuteCallbackHandlers", 28, new[] { typeof(bool) })]
        [TestCase(typeof(ExampleMethods), "TestSimpleException", 3, new Type[0])]
        [TestCase(typeof(ExampleMethods), "FakeOpCode", 6, new Type[0])]
        public void TestGenerateGroups(Type aType, string aMethodName, int aExpectedGroups, Type[] aArgs)
        {
            if (aArgs is null)
            {
                aArgs = Array.Empty<Type>();
            }
            var method = aType.GetMethod(aMethodName, 0, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, aArgs, null);
            var methodBase = new Il2cpuMethodInfo(method, 1, Il2cpuMethodInfo.TypeEnum.Normal, null);

            var appAssembler = new AppAssembler(null, new VoidTextWriter(), "")
            {
                DebugMode = Cosmos.Build.Common.DebugMode.None
            };

            var ilReader = new ILReader();
            var opCodes = ilReader.ProcessMethod(method);

            var mSequence = appAssembler.GenerateDebugSequencePoints(methodBase, Cosmos.Build.Common.DebugMode.None);

            var iMethod = new ILMethod(opCodes, mSequence);
            var groups = ILGroup.GenerateGroups(iMethod, mSequence);
            Assert.AreEqual(aExpectedGroups, groups.Count);
        }

        [Test]
        [TestCase(typeof(string), "Clone", null)]
        [TestCase(typeof(string), "IndexOf", new[] { typeof(string), typeof(int), typeof(int), typeof(StringComparison) })]
        [TestCase(typeof(CancellationTokenSource), "ExecuteCallbackHandlers", new[] { typeof(bool) })]
        [TestCase(typeof(BitConverter), "GetBytes", new[] { typeof(long) })]
        [TestCase(typeof(Hashtable), "Insert", new[] { typeof(object), typeof(object), typeof(bool) })]
        [TestCase(typeof(ExampleMethods), "TestSimpleException", new Type[0])]
        [TestCase(typeof(TypedReference), "GetHashCode", new Type[0])]

        public void TestStackAnalysis(Type aType, string aMethodName, Type[] aArgs)
        {
            if (aArgs is null)
            {
                aArgs = Array.Empty<Type>();
            }

            var method = aType.GetMethod(aMethodName, 0, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, aArgs, null);
            var methodBase = new Il2cpuMethodInfo(method, 1, Il2cpuMethodInfo.TypeEnum.Normal, null);

            var appAssembler = new AppAssembler(null, new VoidTextWriter(), "")
            {
                DebugMode = Cosmos.Build.Common.DebugMode.None
            };

            var ilReader = new ILReader();
            var opCodes = ilReader.ProcessMethod(method);

            var mSequence = appAssembler.GenerateDebugSequencePoints(methodBase, Cosmos.Build.Common.DebugMode.None);

            var iMethod = new ILMethod(opCodes, mSequence);
            Assert.DoesNotThrow(() => iMethod.Analyse());
        }
    }
}
