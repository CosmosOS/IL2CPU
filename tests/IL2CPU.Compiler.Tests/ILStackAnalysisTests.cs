using System;
using System.IO;
using System.Text;
using System.Threading;
using Cosmos.IL2CPU;
using Cosmos.IL2CPU.MethodAnalysis;
using NUnit.Framework;
using System.Reflection;
using System.Linq;

namespace IL2CPU.Compiler.Tests
{
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
        [TestCase(typeof(CancellationTokenSource), "ExecuteCallbackHandlers", 32, new[] { typeof(bool) })]
        public void TestGenerateGroups(Type aType, string aMethodName, int aExpectedGroups, Type[] aArgs)
        {
            if (aArgs is null)
            {
                aArgs = Array.Empty<Type>();
            }
            var method = aType.GetMethod(aMethodName, 0, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, aArgs, null);
            var methodBase = new _MethodInfo(method, 1, _MethodInfo.TypeEnum.Normal, null);

            var appAssembler = new AppAssembler(null, new VoidTextWriter())
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
        public void TestStackAnalysis(Type aType, string aMethodName, Type[] aArgs)
        {
            if (aArgs is null)
            {
                aArgs = Array.Empty<Type>();
            }
            var method = aType.GetMethod(aMethodName, 0, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, aArgs, null);
            var methodBase = new _MethodInfo(method, 1, _MethodInfo.TypeEnum.Normal, null);

            var appAssembler = new AppAssembler(null, new VoidTextWriter())
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
