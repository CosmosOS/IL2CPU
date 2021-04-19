//using System;
//using System.IO;
//using System.Text;
//using System.Threading;
//using Cosmos.IL2CPU;
//using NUnit.Framework;

//namespace IL2CPU.Compiler.Tests
//{
//    public class VoidTextWriter : TextWriter
//    {
//        public override void Write(string value) { }
//        public override void Flush() { }
//        public override Encoding Encoding => throw new NotImplementedException();
//    }

//    [TestFixture(TestOf = typeof(AppAssembler))]
//    public class ILStackAnalysisTests
//    {
//        [Test]
//        [TestCase(typeof(Timer), "Dispose", 1)]
//        public void TestGenerateGroups(Type aType, string methodName, int expectedGroups)
//        {
//            var method = aType.GetMethod(methodName);
//            var methodBase = new _MethodInfo(method, 1, _MethodInfo.TypeEnum.Normal, null);

//            var appAssembler = new AppAssembler(null, new VoidTextWriter())
//            {
//                DebugMode = Cosmos.Build.Common.DebugMode.None
//            };

//            var ilReader = new ILReader();
//            var opCodes = ilReader.ProcessMethod(method);

//            appAssembler.AnalyseMethodOpCodes(methodBase, opCodes);
//        }
//    }
//}
