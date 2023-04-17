using System;
using System.Reflection;
using XSharp;
using XSharp.Assembler.x86;
using static XSharp.XSRegisters;

/* Add.Ovf is signed integer addition with check for overflow */
namespace Cosmos.IL2CPU.CIL.Instructions
{
  [OpCode(ILOpCode.Code.Add_Ovf)]
  public class Add_Ovf : ILOp
  {
    public Add_Ovf(XSharp.Assembler.Assembler aAsmblr)
        : base(aAsmblr)
    {
    }

    public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
    {
      var xType = aOpCode.StackPopTypes[0];
      var xSize = SizeOfType(xType);
      var xIsFloat = TypeIsFloat(xType);

      if (xIsFloat)
      {
        throw new Exception("Cosmos.IL2CPU.x86->IL->Add_Ovf.cs->Error: Expected signed integer operands but get float!");
      }

      if (xSize > 8)
      {
        //EmitNotImplementedException( Assembler, aServiceProvider, "Size '" + xSize.Size + "' not supported (add)", aCurrentLabel, aCurrentMethodInfo, aCurrentOffset, aNextLabel );
        throw new NotImplementedException("Cosmos.IL2CPU.x86->IL->Add_Ovf.cs->Error: StackSize > 8 not supported");
      }
      else
      {
        var xBaseLabel = GetLabel(aMethod, aOpCode) + ".";
        var xSuccessLabel = xBaseLabel + "Success";
        if (xSize > 4) // long
        {
          XS.Pop(EDX); // low part
          XS.Pop(EAX); // high part
          XS.Add(ESP, EDX, destinationIsIndirect: true);
          XS.AddWithCarry(ESP, EAX, destinationDisplacement: 4);

        }
        else //integer
        {

          XS.Pop(EAX);
          XS.Add(ESP, EAX, destinationIsIndirect: true);
        }

        // Let's check if we add overflow and if so throw OverflowException
        XS.Jump(ConditionalTestEnum.NoOverflow, xSuccessLabel);
        if (xSize > 4) // Hack to stop stack corruption
        {
          XS.Add(ESP, 8);
        }
        else
        {
          XS.Add(ESP, 4);
        }
        Call.DoExecute(Assembler, aMethod, typeof(ExceptionHelper).GetMethod("ThrowOverflow", BindingFlags.Static | BindingFlags.Public), aOpCode, GetLabel(aMethod, aOpCode), xSuccessLabel, DebugEnabled);
        XS.Label(xSuccessLabel);
      }
    }
  }
}
