using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using Cosmos.IL2CPU;
using Cosmos.IL2CPU.X86.IL;
using NUnit.Framework;

namespace IL2CPU.Compiler.Tests
{
    public class LdargTests
    {
        [Test]
        [TestCase(0, 32)]
        [TestCase(1, 24)]
        [TestCase(2, 20)]
        public void GetArgumentDisplacement(int aIndex, int aExpected)
        {
            var xDeclaringType = typeof(SystemColorTest);
            var xMethod = xDeclaringType.GetMethod("GetPointColor");
            var xReturnType = xMethod.ReturnType;
            var xParameterTypes = xMethod.GetParameters().Select(p=>p.ParameterType).ToArray();
            bool xIsStatic = xMethod.IsStatic;

            int xActual = Ldarg.GetArgumentDisplacement((ushort)aIndex, xDeclaringType, xParameterTypes, xReturnType, xIsStatic);
            Assert.AreEqual(aExpected, xActual);
        }
    }

    internal class SystemColorTest
    {
        public Color GetPointColor(int x, int y)
        {
            return Color.White;
        }
    }
}
