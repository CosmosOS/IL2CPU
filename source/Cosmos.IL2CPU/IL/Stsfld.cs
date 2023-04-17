using System;
using System.Linq;
using System.Reflection;
using IL2CPU.API;
using XSharp;
using XSharp.Assembler;
using CPU = XSharp.Assembler.x86;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.IL
{
    [global::Cosmos.IL2CPU.OpCode(ILOpCode.Code.Stsfld)]
    public class Stsfld : ILOp
    {
        public Stsfld(XSharp.Assembler.Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xType = aMethod.MethodBase.DeclaringType;
            var xOpCode = (ILOpCodes.OpField)aOpCode;
            FieldInfo xField = xOpCode.Value;
            var xIsReferenceType = IsReferenceType(xField.FieldType);

            // call cctor:
            var xCctor = xField.DeclaringType.GetConstructors(BindingFlags.Static | BindingFlags.NonPublic).SingleOrDefault();
            if (xCctor != null)
            {
                XS.Call(LabelName.Get(xCctor));
                EmitExceptionLogic(Assembler, aMethod, aOpCode, true, null, ".AfterCCTorExceptionCheck");
                XS.Label(".AfterCCTorExceptionCheck");
            }

            uint xSize = SizeOfType(xField.FieldType);

            XS.Comment("Type = '" + xField.FieldType.FullName + "'");
            uint xOffset = 0;

            var xFields = xField.DeclaringType.GetFields();

            foreach (FieldInfo xInfo in xFields)
            {
                if (xInfo == xField)
                {
                    break;
                }

                xOffset += SizeOfType(xInfo.FieldType);
            }
            string xDataName = LabelName.GetStaticFieldName(xField);

            if (xIsReferenceType)
            {
                var name = ElementReference.New(xDataName).Name;
                XS.Add(ESP, 4);

                // GC clean up old object
                XS.Compare(name, 0, destinationIsIndirect: true, destinationDisplacement: 4);
                XS.Jump(CPU.ConditionalTestEnum.Equal, ".AfterGC");
                XS.Push(name, isIndirect: true, displacement: 4); // push object as pointer to send to DecRootCount
                XS.Call(LabelName.Get(GCImplementationRefs.DecRootCountRef));
                XS.Label(".AfterGC");

                XS.Pop(EAX);
                XS.Set(name, EAX, destinationIsIndirect: true, destinationDisplacement: 4);

                // Update GC for new object
                XS.Compare(name, 0, destinationIsIndirect: true, destinationDisplacement: 4);
                XS.Jump(CPU.ConditionalTestEnum.Equal, ".SecondAfterGC");
                XS.Push(name, isIndirect: true, displacement: 4); // push object as pointer/uint to send to IncRootCount

                XS.Call(LabelName.Get(GCImplementationRefs.IncRootCountRef));
                XS.Label(".SecondAfterGC");

                return;
            }

            // value types

            if(!xField.FieldType.IsPointer && !xField.FieldType.IsPrimitive && !xField.FieldType.IsEnum)
            {
                // let clean up object deal with it
                XS.Push(xDataName, isIndirect: true, displacement: 4);
                XS.Push(GetTypeIDLabel(xField.FieldType), isIndirect: true);
                XS.Call(LabelName.Get(GCImplementationRefs.DecRootCountsInStructRef));
            }

            for (int i = 0; i < xSize / 4; i++)
            {
                XS.Pop(EAX);
                new CPU.Mov { DestinationRef = ElementReference.New(xDataName, i * 4), DestinationIsIndirect = true, SourceReg = CPU.RegistersEnum.EAX };
            }
            switch (xSize % 4)
            {
                case 1:
                    {
                        XS.Pop(EAX);
                        new CPU.Mov { DestinationRef = ElementReference.New(xDataName, (int)(xSize / 4 * 4)), DestinationIsIndirect = true, SourceReg = CPU.RegistersEnum.AL };
                        break;
                    }
                case 2:
                    {
                        XS.Pop(EAX);
                        new CPU.Mov { DestinationRef = ElementReference.New(xDataName, (int)(xSize / 4 * 4)), DestinationIsIndirect = true, SourceReg = CPU.RegistersEnum.AX };
                        break;
                    }
                case 0:
                    {
                        break;
                    }
                default:
                    throw new NotImplementedException();
            }

            if (!xField.FieldType.IsPointer && !xField.FieldType.IsPrimitive && !xField.FieldType.IsEnum)
            {
                // let clean up object deal with it
                XS.Push(xDataName, isIndirect: true, displacement: 4);
                XS.Push(GetTypeIDLabel(xField.FieldType), isIndirect: true);
                XS.Call(LabelName.Get(GCImplementationRefs.IncRootCountsInStructRef));
            }
        }

    }
}
