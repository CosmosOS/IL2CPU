﻿using System;
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
        // Total args size                      - 16 bytes
        [TestCase(0, 28)] // $this - 8 bytes
        [TestCase(1, 20)] // x     - 4 bytes
        [TestCase(2, 16)] // y     - 4 bytes
        // Color is 24 bytes so                 - 8 bytes (extra)
        public void GetArgumentDisplacement_Instance_Color_This_Int_Int(int aIndex, int aExpected)
        {
            var xDeclaringType = typeof(TestMethods);
            var xMethod = xDeclaringType.GetMethod("Color_Int_Int_Method");
            RunTest(aIndex, aExpected, xDeclaringType, xMethod);
        }

        [Test]
        // Total args size                   - 12 bytes
        [TestCase(0, 16)] // a  - 4 bytes
        [TestCase(1, 12)] // b  - 4 bytes
        [TestCase(2, 8)]  // c  - 4 bytes
        // Void is 0 bytes so                - 0 bytes (extra)
        public void GetArgumentDisplacement_Static_Void_Int_Int_Int(int aIndex, int aExpected)
        {
            var xDeclaringType = typeof(TestMethods);
            var xMethod = xDeclaringType.GetMethod("Static_Int_Int_Int_Method");
            RunTest(aIndex, aExpected, xDeclaringType, xMethod);
        }

        [Test]
        // Total args size                       - 24 bytes
        [TestCase(0, 28)] // this   - 8 bytes
        [TestCase(1, 20)] // a      - 4 bytes
        [TestCase(2, 16)] // b      - 4 bytes
        [TestCase(3, 12)] // c      - 8 bytes
        // Int is 4 bytes so                     - 0 bytes (extra)
        public void GetArgumentDisplacement_Instance_Int_Int_Int_Object(int aIndex, int aExpected)
        {
            var xDeclaringType = typeof(TestMethods);
            var xMethod = xDeclaringType.GetMethod("Instance_Int_Int_Object_Method");
            RunTest(aIndex, aExpected, xDeclaringType, xMethod);
        }


        [Test]
        // Total args size                       - 8 bytes 
        [TestCase(0, 12)] // this   - 8 bytes 
        // Void is 0 bytes so                    - 0 bytes (extra)
        public void GetArgumentDisplacement_Instance_Method(int aIndex, int aExpected)
        {
            var xDeclaringType = typeof(TestMethods);
            var xMethod = xDeclaringType.GetMethod("Instance_Method");
            RunTest(aIndex, aExpected, xDeclaringType, xMethod);
        }


        [Test]
        // Total args size                  - 8 bytes
        [TestCase(0, 12)] // a - 8 bytes
        // Void is 0 bytes so               - 0 bytes (extra)
        public void GetArgumentDisplacement_Static_Object_Method(int aIndex, int aExpected)
        {
            var xDeclaringType = typeof(TestMethods);
            var xMethod = xDeclaringType.GetMethod("Static_Void_Object_Method");
            RunTest(aIndex, aExpected, xDeclaringType, xMethod);
        }

        [Test]
        [TestCase(0, 28)] // $this - 8 bytes
        // Struct24 is 24 bytes so              - 16 bytes (extra)
        public void GetArgumentDisplacement_Instance_Struct24_Method(int aIndex, int aExpected)
        {
            var xDeclaringType = typeof(TestMethods);
            var xMethod = xDeclaringType.GetMethod("Instance_Struct24_Method");
            RunTest(aIndex, aExpected, xDeclaringType, xMethod);
        }

        [Test]
        [TestCase(0, 28)] // $this  - 8 bytes
        [TestCase(1, 20)] // a      - 4 bytes
        // Struct24 is 24 bytes so               - 12 bytes (extra)
        public void GetArgumentDisplacement_Instance_Struct24_Int_Method(int aIndex, int aExpected)
        {
            var xDeclaringType = typeof(TestMethods);
            var xMethod = xDeclaringType.GetMethod("Instance_Struct24_Int_Method");
            RunTest(aIndex, aExpected, xDeclaringType, xMethod);
        }

        private static void RunTest(int aIndex, int aExpected, Type aDeclaringType, MethodInfo aMethod)
        {
            var xReturnType = aMethod.ReturnType;
            var xParameterTypes = aMethod.GetParameters().Select(p => p.ParameterType).ToArray();
            bool xIsStatic = aMethod.IsStatic;

            int xActual = Ldarg.GetArgumentDisplacement((ushort)aIndex, aDeclaringType, xParameterTypes, xReturnType, xIsStatic);
            Assert.AreEqual(aExpected, xActual);
        }
    }

    internal class TestMethods
    {
        public Color Color_Int_Int_Method(int x, int y)
        {
            return Color.White;
        }

        public static void Static_Int_Int_Int_Method(int a, int b, int c)
        {

        }

        public int Instance_Int_Int_Object_Method(int a, int b, object c)
        {
            return 0;
        }

        public void Instance_Method()
        {

        }

        public static object Static_Void_Object_Method(object a)
        {
            return a;
        }

        public Struct24 Instance_Struct24_Method()
        {
            return new Struct24();
        }

        public Struct24 Instance_Struct24_Int_Method(int a)
        {
            return new Struct24();
        }
    }

    internal struct Struct24
    {
        object a;
        object b;
        object c;
    }
}
