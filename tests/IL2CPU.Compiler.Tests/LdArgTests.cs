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
        [TestCase(0, 32)] // $this
        [TestCase(1, 24)] // x
        [TestCase(2, 20)] // y
        // Color is 24 bytes
        public void GetArgumentDisplacement_Instance_Color_This_Int_Int(int aIndex, int aExpected)
        {
            var xDeclaringType = typeof(TestMethods);
            var xMethod = xDeclaringType.GetMethod("Color_Int_Int_Method");
            RunTest(aIndex, aExpected, xDeclaringType, xMethod);
        }

        [Test]
        [TestCase(0, 16)] // a
        [TestCase(1, 12)] // b
        [TestCase(2, 8)]  // c
        //Void is 0
        public void GetArgumentDisplacement_Static_Void_Int_Int_Int(int aIndex, int aExpected)
        {
            var xDeclaringType = typeof(TestMethods);
            var xMethod = xDeclaringType.GetMethod("Static_Int_Int_Int_Method");
            RunTest(aIndex, aExpected, xDeclaringType, xMethod);
        }

        [Test]
        [TestCase(0, 28)] // this
        [TestCase(1, 20)] // a
        [TestCase(2, 16)] // b
        [TestCase(3, 12)] // c
        //int is 4
        public void GetArgumentDisplacement_Instance_Int_Int_Int_Object(int aIndex, int aExpected)
        {
            var xDeclaringType = typeof(TestMethods);
            var xMethod = xDeclaringType.GetMethod("Instance_Int_Int_Object_Method");
            RunTest(aIndex, aExpected, xDeclaringType, xMethod);
        }


        [Test]
        [TestCase(0, 12)] // this
        //void is 0
        public void GetArgumentDisplacement_Instance_Method(int aIndex, int aExpected)
        {
            var xDeclaringType = typeof(TestMethods);
            var xMethod = xDeclaringType.GetMethod("Instance_Method");
            RunTest(aIndex, aExpected, xDeclaringType, xMethod);
        }


        [Test]
        [TestCase(0, 12)] // a
        //void is 0
        public void GetArgumentDisplacement_Static_Object_Method(int aIndex, int aExpected)
        {
            var xDeclaringType = typeof(TestMethods);
            var xMethod = xDeclaringType.GetMethod("Static_Void_Object_Method");
            RunTest(aIndex, aExpected, xDeclaringType, xMethod);
        }

        [Test]
        [TestCase(0, 28)] // this
        //Struct is 24
        public void GetArgumentDisplacement_Struc24_Instance_Method(int aIndex, int aExpected)
        {
            var xDeclaringType = typeof(TestMethods);
            var xMethod = xDeclaringType.GetMethod("Struct24_Instance_Method");
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

        public object Static_Void_Object_Method(object a)
        {
            return a;
        }

        public Struct24 Struct24_Instance_Method()
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
