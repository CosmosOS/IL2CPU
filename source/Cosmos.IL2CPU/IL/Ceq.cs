using System;
using XSharp;
using XSharp.Assembler;
using XSharp.Assembler.x86;
using static XSharp.Assembler.x86.SSE.ComparePseudoOpcodes;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
  [Cosmos.IL2CPU.OpCode(ILOpCode.Code.Ceq)]
  public class Ceq : ILOp
  {
    public Ceq(XSharp.Assembler.Assembler aAsmblr)
      : base(aAsmblr)
    {
    }

    public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
    {
      var xStackItem = aOpCode.StackPopTypes[0];
      var xStackItemSize = SizeOfType(xStackItem);
      var xStackItemIsFloat = TypeIsFloat(xStackItem);
      var xStackItem2 = aOpCode.StackPopTypes[1];
      var xStackItem2Size = SizeOfType(xStackItem2);
      var xStackItem2IsFloat = TypeIsFloat(xStackItem2);
      var xSize = Math.Max(xStackItemSize, xStackItem2Size);

      var xNextLabel = GetLabel(aMethod, aOpCode.NextPosition);

      if (xSize > 8)
      {
        throw new Exception("Cosmos.IL2CPU.x86->IL->Ceq.cs->Error: StackSizes > 8 not supported");
      }
      else if (xSize <= 4)
      {
        if (xStackItemIsFloat) // float
        {
          XS.SSE.MoveSS(XMM0, RSP, sourceIsIndirect: true);
          XS.Add(RSP, 4);
          XS.SSE.MoveSS(XMM1, RSP, sourceIsIndirect: true);
          XS.SSE.CompareSS(XMM1, XMM0, comparision: Equal);
          XS.MoveD(RBX, XMM1);
          XS.And(RBX, 1);
          XS.Set(RSP, RBX, destinationIsIndirect: true);
        }
        else
        {
          XS.Xor(RBX, RBX);
          XS.Pop(RCX);
          XS.Pop(RAX);
          XS.Compare(RAX, RCX);
          XS.SetByteOnCondition(ConditionalTestEnum.Equal, BL);
          XS.Push(RBX);
        }
      }
      else if (xSize > 4)
      {
        if (xStackItemIsFloat)
        {
          // Please note that SSE supports double operations only from version 2
          XS.SSE2.MoveSD(XMM0, RSP, sourceIsIndirect: true);
          // Increment ESP to get the value of the next double
          XS.Add(RSP, 8);
          XS.SSE2.MoveSD(XMM1, RSP, sourceIsIndirect: true);
          XS.SSE2.CompareSD(XMM1, XMM0, comparision: Equal);
          XS.MoveD(RBX, XMM1);
          XS.And(RBX, 1);
          // We need to move the stack pointer of 4 Byte to "eat" the second double that is yet in the stack or we get a corrupted stack!
          XS.Add(RSP, 4);
          XS.Set(RSP, RBX, destinationIsIndirect: true);
        }
        else
        {
          if (IsReferenceType(xStackItem) && IsReferenceType(xStackItem2))
          {
            XS.Comment(xStackItem.Name);
            XS.Add(RSP, 4);
            XS.Pop(RAX);

            XS.Comment(xStackItem2.Name);
            XS.Add(RSP, 4);
            XS.Pop(RBX);

            XS.Compare(RAX, RBX);
            XS.Jump(ConditionalTestEnum.NotEqual, Label.LastFullLabel + ".False");

            // equal
            XS.Push(1);
            XS.Jump(xNextLabel);
            XS.Label(Label.LastFullLabel + ".False");
            //not equal
            XS.Push(0);
            XS.Jump(xNextLabel);
          }
          else
          {
            XS.Pop(RAX);
            XS.Compare(RAX, RSP, sourceDisplacement: 4);
            XS.Pop(RAX);
            XS.Jump(ConditionalTestEnum.NotEqual, Label.LastFullLabel + ".False");
            XS.Xor(RAX, RSP, sourceDisplacement: 4);
            XS.Jump(ConditionalTestEnum.NotZero, Label.LastFullLabel + ".False");

            //they are equal
            XS.Add(RSP, 8);
            XS.Push(1);
            XS.Jump(xNextLabel);
            XS.Label(Label.LastFullLabel + ".False");
            //not equal
            XS.Add(RSP, 8);
            XS.Push(0);
            XS.Jump(xNextLabel);

          }
        }
      }
      else
      {
        throw new Exception("Cosmos.IL2CPU.x86->IL->Ceq.cs->Error: Case not handled!");
      }
    }
  }
}
