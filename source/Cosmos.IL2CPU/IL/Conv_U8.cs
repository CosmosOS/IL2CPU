using System;

using XSharp;
using XSharp.Assembler;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    /// <summary>
    /// Convert to unsigned int64, pushing int64 on stack.
    /// </summary>
    [OpCode(ILOpCode.Code.Conv_U8)]
    public class Conv_U8 : ILOp
    {
        public Conv_U8(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
    {
      var xSource = aOpCode.StackPopTypes[0];
      var xSourceSize = SizeOfType(xSource);
      var xSourceIsFloat = TypeIsFloat(xSource);

      DoExecute(aMethod, xSource, xSourceSize, xSourceIsFloat);
    }

    public static void DoExecute(Il2cpuMethodInfo aMethod, Type xSource, uint xSourceSize, bool xSourceIsFloat)
    {
      if (IsReferenceType(xSource))
      {
        // todo: Stop GC tracking
        XS.Add(ESP, SizeOfType(typeof(UIntPtr)));

        // todo: x64
        XS.Pop(EAX);
        XS.Push(0);
        XS.Push(EAX);
      }
      else if (IsByRef(xSource))
      {
        // todo: Stop GC tracking
        throw new NotImplementedException($"Error compiling '{GetLabel(aMethod)}': conv.u8 not implemented for byref types!");
      }
      else if (xSourceSize <= 4)
      {
        if (xSourceIsFloat)
        {
          XS.FPU.FloatLoad(ESP, destinationIsIndirect: true, size: RegisterSize.Int32);
          XS.Sub(ESP, 4);
          XS.FPU.IntStoreWithTruncate(ESP, isIndirect: true, size: RegisterSize.Long64);
        }
        else
        {
          XS.Pop(EAX);
          XS.Push(0);
          XS.Push(EAX);
        }
      }
      else if (xSourceSize <= 8)
      {
        if (xSourceIsFloat)
        {
          XS.FPU.FloatLoad(ESP, destinationIsIndirect: true, size: RegisterSize.Long64);
          /* The sign of the value should not be changed a negative value is simply converted to its corresponding ulong value */
          //XS.FPU.FloatAbs();
          XS.FPU.IntStoreWithTruncate(ESP, isIndirect: true, size: RegisterSize.Long64);
        }
      }
      else
      {
        throw new NotImplementedException("Cosmos.IL2CPU.x86->IL->Conv_U8.cs->Error: StackSize > 8 not supported");
      }
    }
  }
}
