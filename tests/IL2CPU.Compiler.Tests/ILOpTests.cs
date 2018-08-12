using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;

using Moq;
using NUnit.Framework;

using IL2CPU.Reflection;

using Cosmos.IL2CPU;

namespace IL2CPU.Compiler.Tests
{
    [TestFixture(TestOf = typeof(ILOp))]
    public class ILOpTests
    {
        private MetadataContext _metadataContext;

        [SetUp]
        public void SetupMetadataContext()
        {
            _metadataContext = MetadataContext.FromAssemblyPaths(
                new string[]
                {
                    typeof(byte).Assembly.Location,
                    typeof(string).Assembly.Location,
                    typeof(Color).Assembly.Location
                });
        }

        [Test]
        public void GetFieldsInfo_ForSequentialLayoutValueType_ReturnsCorrectOffsets()
        {
            var valueType = MockDefaultValueType(
                MockField(TypeOf(BclType.Byte)),
                MockField(TypeOf(BclType.String)));

            var fieldsInfo = ILOp.GetFieldsInfo(valueType, false);

            Assert.That(fieldsInfo, Has.Count.EqualTo(2));

            Assert.That(fieldsInfo[0].Offset, Is.EqualTo(0));
            Assert.That(fieldsInfo[1].Offset, Is.EqualTo(4));
        }

        [Test]
        public void GetFieldsInfo_ForSequentialLayoutValueTypeWithPackEqualTo2_ReturnsCorrectOffsets()
        {
            var valueType = MockSequentialValueType(
                2,
                MockField(TypeOf(BclType.Byte)),
                MockField(TypeOf(BclType.String)));

            var fieldsInfo = ILOp.GetFieldsInfo(valueType, false);

            Assert.That(fieldsInfo, Has.Count.EqualTo(2));

            Assert.That(fieldsInfo[0].Offset, Is.EqualTo(0));
            Assert.That(fieldsInfo[1].Offset, Is.EqualTo(2));
        }

        [Test]
        public void GetFieldsInfo_ForExplicitLayoutValueType_ReturnsCorrectOffsets()
        {
            var valueType = MockExplicitValueType(
                MockField(TypeOf(BclType.Byte), 3),
                MockField(TypeOf(BclType.String), 3));

            var fieldsInfo = ILOp.GetFieldsInfo(valueType, false);

            Assert.That(fieldsInfo, Has.Count.EqualTo(2));

            Assert.That(fieldsInfo[0].Offset, Is.EqualTo(3));
            Assert.That(fieldsInfo[1].Offset, Is.EqualTo(3));
        }

        [Test]
        public void GetFieldsInfo_ForColorStruct_ReturnsCorrectOffsets()
        {
            var fieldsInfo = ILOp.GetFieldsInfo(TypeOf<Color>(), false);

            Assert.That(fieldsInfo, Has.Count.EqualTo(4));

            Assert.That(fieldsInfo[0].Offset, Is.EqualTo(0));
            Assert.That(fieldsInfo[1].Offset, Is.EqualTo(8));
            Assert.That(fieldsInfo[2].Offset, Is.EqualTo(16));
            Assert.That(fieldsInfo[3].Offset, Is.EqualTo(18));
        }

        [Test]
        public void SizeOfType_ForColorStruct_Returns24()
        {
            var size = ILOp.SizeOfType(TypeOf<Color>());
            Assert.That(size, Is.EqualTo(24));
        }

        private TypeInfo MockDefaultValueType(params Mock<FieldInfo>[] fieldMocks) =>
            MockValueTypeFull(fieldMocks: fieldMocks);
        private TypeInfo MockSequentialValueType(int pack, params Mock<FieldInfo>[] fieldMocks) =>
            MockValueTypeFull(pack: pack, fieldMocks: fieldMocks);
        private TypeInfo MockExplicitValueType(params Mock<FieldInfo>[] fieldMocks) =>
            MockValueTypeFull(LayoutKind.Explicit, fieldMocks: fieldMocks);
        private TypeInfo MockAutoValueType(int pack, params Mock<FieldInfo>[] fieldMocks) =>
            MockValueTypeFull(LayoutKind.Auto, pack, fieldMocks: fieldMocks);

        private TypeInfo MockValueTypeFull(
            LayoutKind layoutKind = LayoutKind.Sequential,
            int pack = 0,
            int size = 0,
            params Mock<FieldInfo>[] fieldMocks)
        {
            var typeMock = new Mock<TypeInfo>();
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

            typeMock.Setup(t => t.IsValueType).Returns(true);
            typeMock.Setup(t => t.StructLayoutAttribute).Returns(structLayoutAttribute);
            typeMock.Setup(t => t.GetFields(f => !f.IsStatic)).Returns(fieldInfos);

            return typeMock.Object;
        }

        private Mock<FieldInfo> MockField(TypeInfo fieldType, int? fieldOffset = null)
        {
            var fieldInfoMock = new Mock<FieldInfo>();
            fieldInfoMock.Setup(i => i.FieldType).Returns(fieldType);

            if (fieldOffset.HasValue)
            {
                fieldInfoMock.Setup(f => f.Offset).Returns(fieldOffset.Value);
            }

            return fieldInfoMock;
        }

        private TypeInfo TypeOf(BclType bclType) => _metadataContext.GetBclType(bclType);

        private TypeInfo TypeOf<T>() => _metadataContext.ImportType<T>();
    }
}
