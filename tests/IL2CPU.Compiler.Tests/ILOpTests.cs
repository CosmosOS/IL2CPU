using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

using Moq;
using NUnit.Framework;

using Cosmos.IL2CPU;

namespace IL2CPU.Compiler.Tests
{
    [TestFixture(TestOf = typeof(ILOp))]
    public class ILOpTests
    {
        [Test]
        public void GetFieldsInfo_ForSequentialLayoutValueType_ReturnsCorrectOffsets()
        {
            var valueType = MockDefaultValueType(MockField(typeof(byte)), MockField(typeof(string)));
            var fieldsInfo = ILOp.GetFieldsInfo(valueType, false);

            Assert.That(fieldsInfo, Has.Count.EqualTo(2));

            Assert.That(fieldsInfo[0].Offset, Is.EqualTo(0));
            Assert.That(fieldsInfo[1].Offset, Is.EqualTo(4));
        }

        [Test]
        public void GetFieldsInfo_ForSequentialLayoutValueTypeWithPackEqualTo2_ReturnsCorrectOffsets()
        {
            var valueType = MockSequentialValueType(2, MockField(typeof(byte)), MockField(typeof(string)));
            var fieldsInfo = ILOp.GetFieldsInfo(valueType, false);

            Assert.That(fieldsInfo, Has.Count.EqualTo(2));

            Assert.That(fieldsInfo[0].Offset, Is.EqualTo(0));
            Assert.That(fieldsInfo[1].Offset, Is.EqualTo(2));
        }

        [Test]
        public void GetFieldsInfo_ForExplicitLayoutValueType_ReturnsCorrectOffsets()
        {
            var valueType = MockExplicitValueType(MockField(typeof(byte), 3), MockField(typeof(string), 3));
            var fieldsInfo = ILOp.GetFieldsInfo(valueType, false);

            Assert.That(fieldsInfo, Has.Count.EqualTo(2));

            Assert.That(fieldsInfo[0].Offset, Is.EqualTo(3));
            Assert.That(fieldsInfo[1].Offset, Is.EqualTo(3));
        }

        [Test]
        public void GetFieldsInfo_ForColorStruct_ReturnsCorrectOffsets()
        {
            var fieldsInfo = ILOp.GetFieldsInfo(typeof(Color), false);

            Assert.That(fieldsInfo, Has.Count.EqualTo(4));

            Assert.That(fieldsInfo[0].Offset, Is.EqualTo(0));
            Assert.That(fieldsInfo[1].Offset, Is.EqualTo(8));
            Assert.That(fieldsInfo[2].Offset, Is.EqualTo(16));
            Assert.That(fieldsInfo[3].Offset, Is.EqualTo(18));
        }

        [Test]
        public void SizeOfType_ForColorStruct_Returns24()
        {
            var size = ILOp.SizeOfType(typeof(Color));
            Assert.That(size, Is.EqualTo(24));
        }

        private Type MockDefaultValueType(params Mock<FieldInfo>[] fieldTypes) =>
            MockValueTypeFull(fieldMocks: fieldTypes);
        private Type MockSequentialValueType(int pack, params Mock<FieldInfo>[] fieldTypes) =>
            MockValueTypeFull(pack: pack, fieldMocks: fieldTypes);
        private Type MockExplicitValueType(params Mock<FieldInfo>[] fieldTypes) =>
            MockValueTypeFull(LayoutKind.Explicit, fieldMocks: fieldTypes);
        private Type MockAutoValueType(int pack, params Mock<FieldInfo>[] fieldTypes) =>
            MockValueTypeFull(LayoutKind.Auto, fieldMocks: fieldTypes);

        private Type MockValueTypeFull(
            LayoutKind layoutKind = LayoutKind.Sequential,
            int pack = 0,
            int size = 0,
            params Mock<FieldInfo>[] fieldMocks)
        {
            var typeMock = new Mock<Type>();
            typeMock.CallBase = true;

            var structLayoutAttribute = new StructLayoutAttribute(layoutKind)
            {
                Pack = pack,
                Size = size,
            };

            var fieldInfos = fieldMocks.Select(
                m =>
                {
                    m.Setup(f => f.DeclaringType).Returns(typeMock.Object);
                    return m.Object;
                }).ToArray();

            typeMock.Setup(t => t.BaseType).Returns(typeof(ValueType));
            typeMock.Setup(t => t.StructLayoutAttribute).Returns(structLayoutAttribute);
            typeMock.Setup(
                t => t.GetFields(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    .Returns(fieldInfos);

            return typeMock.Object;
        }

        private Mock<FieldInfo> MockField(Type fieldType, int? fieldOffset = null)
        {
            var fieldInfoMock = new Mock<FieldInfo>();
            fieldInfoMock.Setup(i => i.FieldType).Returns(fieldType);

            if (fieldOffset.HasValue)
            {
                var fieldOffsetAttribute = new FieldOffsetAttribute(fieldOffset.Value);
                fieldInfoMock.Setup(f => f.GetCustomAttributes(typeof(FieldOffsetAttribute), true))
                    .Returns(new[] { fieldOffsetAttribute });
            }

            return fieldInfoMock;
        }
    }
}
