using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace IL2CPU.Reflection.Tests
{
    internal static class TestHelpers
    {
        internal static void SetupAssemblyContext()
        {
            var baseLib = typeof(object).Assembly.Location;
            var runtLib = typeof(FileAttributes).Assembly.Location;
            var listLib = typeof(SortedList<int, int>).Assembly.Location;
            var conLib = typeof(Console).Assembly.Location;
            var iterLib = typeof(Enumerable).Assembly.Location;
            var linqLib = typeof(BinaryExpression).Assembly.Location;
            var jsonLib = typeof(JObject).Assembly.Location;
            var stdLib = Assembly.Load("netstandard").Location;
            var rflLib = Assembly.Load("System.Reflection.Primitives").Location;
            var ctx = new IsolatedAssemblyLoadContext(new[]
            {
                baseLib, runtLib, stdLib, listLib, rflLib, jsonLib, conLib, iterLib, linqLib
            });
            IsolatedAssemblyLoadContext.Default = ctx;
        }
    }
}
