using System;
using System.Linq;
using System.Reflection;

using IL2CPU.API;

using XSharp;
using XSharp.Assembler;
using CPUx86 = XSharp.Assembler.x86;

namespace Cosmos.IL2CPU.X86.IL
{
  [Cosmos.IL2CPU.OpCode(ILOpCode.Code.Stsfld)]
  public class Stsfld : ILOp
  {
    public Stsfld(XSharp.Assembler.Assembler aAsmblr)
        : base(aAsmblr)
    {
    }

    public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
    {
      var xType = aMethod.MethodBase.DeclaringType;
      var xOpCode = (ILOpCodes.OpField)aOpCode;
      FieldInfo xField = xOpCode.Value;
      var xIsReferenceType = IsReferenceType(xField.FieldType);

      // call cctor:
      var xCctor = (xField.DeclaringType.GetConstructors(BindingFlags.Static | BindingFlags.NonPublic)).SingleOrDefault();
      if (xCctor != null)
      {
        XS.Call(LabelName.Get(xCctor));
        ILOp.EmitExceptionLogic(Assembler, aMethod, aOpCode, true, null, ".AfterCCTorExceptionCheck");
        XS.Label(".AfterCCTorExceptionCheck");
      }

      //int aExtraOffset;// = 0;
      //bool xNeedsGC = xField.FieldType.IsClass && !xField.FieldType.IsValueType;
      uint xSize = SizeOfType(xField.FieldType);
      //if( xNeedsGC )
      //{
      //    aExtraOffset = 12;
      //}
      new Comment(Assembler, "Type = '" + xField.FieldType.FullName /*+ "', NeedsGC = " + xNeedsGC*/ );

      uint xOffset = 0;

      var xFields = xField.DeclaringType.GetFields();

      foreach (FieldInfo xInfo in xFields)
      {
        if (xInfo == xField)
          break;

        xOffset += SizeOfType(xInfo.FieldType);
      }
      string xDataName = LabelName.GetStaticFieldName(xField);
      if (xIsReferenceType)
      {
        XS.Add(XSRegisters.ESP, 4);
        XS.Pop(XSRegisters.EAX);
        XS.Set(ElementReference.New(xDataName).Name, XSRegisters.EAX, destinationIsIndirect: true, destinationDisplacement: 4);
        return;
      }
      for (int i = 0; i < (xSize / 4); i++)
      {
        XS.Pop(XSRegisters.EAX);
        new CPUx86.Mov { DestinationRef = ElementReference.New(xDataName, i * 4), DestinationIsIndirect = true, SourceReg = CPUx86.RegistersEnum.EAX };
      }
      switch (xSize % 4)
      {
        case 1:
          {
            XS.Pop(XSRegisters.EAX);
            new CPUx86.Mov { DestinationRef = ElementReference.New(xDataName, (int)((xSize / 4) * 4)), DestinationIsIndirect = true, SourceReg = CPUx86.RegistersEnum.AL };
            break;
          }
        case 2:
          {
            XS.Pop(XSRegisters.EAX);
            new CPUx86.Mov { DestinationRef = XSharp.Assembler.ElementReference.New(xDataName, (int)((xSize / 4) * 4)), DestinationIsIndirect = true, SourceReg = CPUx86.RegistersEnum.AX };
            break;
          }
        case 0:
          {
            break;
          }
        default:
          throw new NotImplementedException();
      }
    }
  }
}
