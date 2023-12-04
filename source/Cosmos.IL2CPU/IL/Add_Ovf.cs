using System;
using XSharp.Assembler.x86;
using XSharp;
using static XSharp.XSRegisters;
using System.Reflection;

/* Add.Ovf is signed integer addition with check for overflow */
namespace Cosmos.IL2CPU.X86.IL
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
          XS.Pop(RDX); // low part
          XS.Pop(RAX); // high part
          XS.Add(RSP, RDX, destinationIsIndirect: true);
          XS.AddWithCarry(RSP, RAX, destinationDisplacement: 4);

        }
        else //integer
        {

          XS.Pop(RAX);
          XS.Add(RSP, RAX, destinationIsIndirect: true);
        }

        // Let's check if we add overflow and if so throw OverflowException
        XS.Jump(ConditionalTestEnum.NoOverflow, xSuccessLabel);
        if (xSize > 4) // Hack to stop stack corruption
        {
          XS.Add(RSP, 8);
        }
        else
        {
          XS.Add(RSP, 4);
        }
        Call.DoExecute(Assembler, aMethod, typeof(ExceptionHelper).GetMethod("ThrowOverflow", BindingFlags.Static | BindingFlags.Public), aOpCode, GetLabel(aMethod, aOpCode), xSuccessLabel, DebugEnabled);
        XS.Label(xSuccessLabel);
      }
    }
  }
}
