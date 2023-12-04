using System;
using CPUx86 = XSharp.Assembler.x86;
using CPU = XSharp.Assembler.x86;
using XSharp.Assembler.x86;
using XSharp.Assembler;
using XSharp.Assembler.x86.SSE;
using XSharp.Assembler.x86.x87;

using XSharp;
using static XSharp.XSRegisters;
using static XSharp.Assembler.x86.SSE.ComparePseudoOpcodes;

namespace Cosmos.IL2CPU.X86.IL
{
	[Cosmos.IL2CPU.OpCode(ILOpCode.Code.Cgt)]
	public class Cgt : ILOp
	{
		public Cgt(XSharp.Assembler.Assembler aAsmblr)
			: base(aAsmblr)
		{
		}

		public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
		{
			var xStackItem = aOpCode.StackPopTypes[0];
			var xStackItemSize = SizeOfType(xStackItem);
			var xStackItemIsFloat = TypeIsFloat(xStackItem);
			if (xStackItemSize > 8)
			{
				//EmitNotImplementedException( Assembler, GetServiceProvider(), "Cgt: StackSizes>8 not supported", CurInstructionLabel, mMethodInfo, mCurrentOffset, NextInstructionLabel );
				throw new NotImplementedException("Cosmos.IL2CPU.x86->IL->Cgt.cs->Error: StackSizes > 8 not supported");
				//return;
			}
			string BaseLabel = GetLabel(aMethod, aOpCode) + ".";
			string LabelTrue = BaseLabel + "True";
			string LabelFalse = BaseLabel + "False";
			var xNextLabel = GetLabel(aMethod, aOpCode.NextPosition);
			if (xStackItemSize > 4)
			{
				// Using SSE registers (that do NOT branch!) This is needed only for long now
#if false
				XS.Set(XSRegisters.ESI, 1);
				// esi = 1
				XS.Xor(XSRegisters.EDI, XSRegisters.EDI);
				// edi = 0
#endif
				if (xStackItemIsFloat)
				{
					// Please note that SSE supports double operations only from version 2
					XS.SSE2.MoveSD(XMM0, RSP, sourceIsIndirect: true);
					// Increment ESP to get the value of the next double
					XS.Add(RSP, 8);
					XS.SSE2.MoveSD(XMM1, RSP, sourceIsIndirect: true);
					XS.SSE2.CompareSD(XMM1, XMM0, comparision: NotLessThanOrEqualTo);
					XS.MoveD(RBX, XMM1);
					XS.And(RBX, 1);
					// We need to move the stack pointer of 4 Byte to "eat" the second double that is yet in the stack or we get a corrupted stack!
					XS.Add(RSP, 4);
					XS.Set(RSP, RBX, destinationIsIndirect: true);
				}
				else
				{
					XS.Set(RSI, 1);
					// esi = 1
					XS.Xor(RDI, RDI);
					// edi = 0

					XS.Pop(RAX);
					XS.Pop(RDX);
					//value2: EDX:EAX
					XS.Pop(RBX);
					XS.Pop(RCX);
					//value1: ECX:EBX

					XS.Compare(RCX, RDX);
					XS.Jump(ConditionalTestEnum.GreaterThan, LabelTrue);
					XS.Jump(ConditionalTestEnum.LessThan, LabelFalse);
					XS.Compare(RBX, RAX);
					XS.Label(LabelTrue);
					new ConditionalMove { Condition = ConditionalTestEnum.Above, DestinationReg = RegistersEnum.EDI, SourceReg = RegistersEnum.ESI };
					XS.Label(LabelFalse);
					XS.Push(RDI);
				}
				/*
				XS.Jump(ConditionalTestEnum.GreaterThan, LabelTrue);
				XS.Label(LabelFalse);
				XS.Push(0);
				XS.Jump(xNextLabel);
				XS.Label(LabelTrue );
				XS.Push(1);*/
			}
			else
			{
				if (xStackItemIsFloat)
				{
					XS.SSE.MoveSS(XMM0, RSP, sourceIsIndirect: true);
					XS.Add(XSRegisters.RSP, 4);
					XS.SSE.MoveSS(XMM1, RSP, sourceIsIndirect: true);
					XS.SSE.CompareSS(XMM1, XMM0, comparision: NotLessThanOrEqualTo);
					XS.MoveD(RBX, XMM1);
					XS.And(XSRegisters.RBX, 1);
					XS.Set(RSP, RBX, destinationIsIndirect: true);
				}
				else
				{
                    XS.Xor(RBX, RBX);
                    XS.Pop(RCX);
                    XS.Pop(RAX);
                    XS.Compare(RAX, RCX);
                    XS.SetByteOnCondition(ConditionalTestEnum.GreaterThan, BL);
                    XS.Push(RBX);
				}
			}
		}
	}
}
