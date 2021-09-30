using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace IL2CPU.Reflection.Tests
{
    [TestFixture]
    public class StandardTests
    {
        [Test]
        public void ShouldTryGetValue()
        {
            var set = new HashSet<Stream>();
            using var mem = new MemoryStream();
            Assert.False(StandardExtensions.TryGetValue(set, mem, out var res));
            Assert.IsNull(res);
            set.Add(mem);
            Assert.True(StandardExtensions.TryGetValue(set, mem, out res));
            Assert.IsNotNull(res);
            Assert.True(res == mem);
        }

        [Test]
        public void ShouldIsAssignableTo()
        {
            Assert.True(typeof(string).IsAssignableTo(typeof(object)));
            Assert.False(typeof(object).IsAssignableTo(typeof(string)));

            Assert.False(typeof(bool).IsAssignableTo(typeof(int)));
            Assert.False(typeof(int).IsAssignableTo(typeof(bool)));

            Assert.True(typeof(Stack<long>).IsAssignableTo(typeof(IReadOnlyCollection<long>)));
            Assert.False(typeof(IReadOnlyCollection<long>).IsAssignableTo(typeof(Stack<long>)));

            Assert.True(typeof(MemoryStream).IsAssignableTo(typeof(IDisposable)));
            Assert.False(typeof(IDisposable).IsAssignableTo(typeof(MemoryStream)));
        }
    }
}
