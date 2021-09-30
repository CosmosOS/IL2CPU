using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace IL2CPU.Reflection.Tests
{
    [TestFixture]
    public class StandardTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void ShouldTryGetValue(bool useNet)
        {
            var set = new HashSet<Stream>();
            using var mem1 = new MemoryStream(new byte[] { 1 });
            Assert.False(useNet ? set.TryGetValue(mem1, out var res) : set.TryGetMyValue(mem1, out res));
            Assert.IsNull(res);
            using var mem2 = new MemoryStream(new byte[] { 2 });
            using var mem3 = new MemoryStream(new byte[] { 3 });
            set.Add(mem1);
            set.Add(mem2);
            set.Add(mem3);
            Assert.True(useNet ? set.TryGetValue(mem2, out res) : set.TryGetMyValue(mem2, out res));
            Assert.IsNotNull(res);
            Assert.AreEqual(2, ((MemoryStream)res).ToArray().Single());
            Assert.True(res == mem2);
            Assert.True(useNet ? set.TryGetValue(mem1, out res) : set.TryGetMyValue(mem1, out res));
            Assert.IsNotNull(res);
            Assert.AreEqual(1, ((MemoryStream)res).ToArray().Single());
            Assert.True(res == mem1);
            Assert.True(useNet ? set.TryGetValue(mem3, out res) : set.TryGetMyValue(mem3, out res));
            Assert.IsNotNull(res);
            Assert.AreEqual(3, ((MemoryStream)res).ToArray().Single());
            Assert.True(res == mem3);
        }

        [TestCase(null, null, null)]
        [TestCase(typeof(int), null, false)]
        [TestCase(null, typeof(int), null)]
        [TestCase(typeof(string), typeof(object), true)]
        [TestCase(typeof(object), typeof(string), false)]
        [TestCase(typeof(bool), typeof(int), false)]
        [TestCase(typeof(int), typeof(bool), false)]
        [TestCase(typeof(ArrayList), typeof(IList), true)]
        [TestCase(typeof(IList), typeof(ArrayList), false)]
        [TestCase(typeof(Stack<long>), typeof(IEnumerable), true)]
        [TestCase(typeof(IEnumerable), typeof(Stack<long>), false)]
        [TestCase(typeof(Stack<long>), typeof(IReadOnlyCollection<long>), true)]
        [TestCase(typeof(IReadOnlyCollection<long>), typeof(Stack<long>), false)]
        [TestCase(typeof(MemoryStream), typeof(IDisposable), true)]
        [TestCase(typeof(IDisposable), typeof(MemoryStream), false)]
        public void ShouldBeAssignableTo(Type first, Type second, bool? expected)
        {
            Assert.AreEqual(expected, first?.IsAssignableTo(second));
            Assert.AreEqual(expected, first?.IsMyAssignableTo(second));
        }
    }
}
