using System;
using System.Collections.Generic;
using System.Text;
using IL2CPU.API;
using NUnit.Framework;

namespace IL2CPU.Compiler.Tests
{
    [TestFixture(TestOf = typeof(LabelName))]
    class LabelNameTest
    {
        static int test = 0;

        [Test]
        public void TestGetTypeFullName()
        {
            Action a = () => { };
            Action<int> a1 = (i) => test++;
            Assert.That(LabelName.GetFullName(a.GetType()) != LabelName.GetFullName(a1.GetType()));

            var c = new { i = 1, n = "Test" };
            var d = new { i = 1, n = "Test" };
            var e = new { n = "Test", i = 1 };
            Assert.That(LabelName.GetFullName(c.GetType()) != null);
            Assert.That(LabelName.GetFullName(c.GetType()) == LabelName.GetFullName(d.GetType()));
        }
    }
}
