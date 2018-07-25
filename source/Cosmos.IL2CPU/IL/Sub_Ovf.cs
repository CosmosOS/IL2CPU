using System;
using CPUx86 = XSharp.Assembler.x86;
using XSharp.Assembler.x86;
using XSharp;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
  [Cosmos.IL2CPU.OpCode(ILOpCode.Code.Sub_Ovf)]
  public class Sub_Ovf : ILOp
  {
    public Sub_Ovf(XSharp.Assembler.Assembler aAsmblr) : base(aAsmblr)
    {
    }

    public override void Execute(_MethodInfo aMethod, ILOpCode aOpCode)
    {
      var xType = aOpCode.StackPopTypes[0];
      var xSize = SizeOfType(xType);
      var xIsFloat = TypeIsFloat(xType);
      if (xIsFloat)
      {
        throw new Exception("Cosmos.IL2CPU.x86->IL->Sub_Ovf.cs->Error: Expected signed integer operands but get float!");
      }

      if (xSize > 8)
      {
        throw new NotImplementedException("Cosmos.IL2CPU.x86->IL->Sub_Ovf.cs->Error: StackSize > 8 not supported");
      }
      else
      {
        var xBaseLabel = GetLabel(aMethod, aOpCode) + ".";
        var xSuccessLabel = xBaseLabel + "Success";
        if (xSize > 4) // long
        {
          XS.Pop(EAX);//low part
          XS.Pop(EDX);//high part
          XS.Sub(ESP, EAX, destinationIsIndirect: true);
          XS.SubWithCarry(ESP, EDX, destinationDisplacement: 4);

        }
        else //integer
        {
          XS.Pop(ECX);//first integer
          XS.Pop(EAX);//second integer
          XS.Sub(EAX, ECX);
          XS.Push(EAX);//push result on stack
        }

        // Let's check if we add overflow and if so throw OverflowException
        XS.Jump(ConditionalTestEnum.NoOverflow, xSuccessLabel);
        ThrowOverflowException();
        XS.Label(xSuccessLabel);
      }
    }
  }
}
