using System;
using Cosmos.IL2CPU.CIL.Utils;
using XSharp;
using XSharp.Assembler.x86;
using static XSharp.XSRegisters;
using static XSharp.Assembler.x86.SSE.ComparePseudoOpcodes;

namespace Cosmos.IL2CPU.CIL.Instructions
{
    /// <summary>
    /// Compares two values. If the first value is less than the second, the integer value 1 (int32) is pushed onto the evaluation stack;
    /// otherwise 0 (int32) is pushed onto the evaluation stack.
    /// </summary>
    public class Clt : ILOp
    {
        public Clt( XSharp.Assembler.Assembler aAsmblr )
            : base( aAsmblr )
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode )
        {
            var xStackItem = aOpCode.StackPopTypes[0];
            var xStackItemSize = SizeOfType(xStackItem);
            var xStackItemIsFloat = TypeIsFloat(xStackItem);
            if( xStackItemSize > 8 )
            {
                //EmitNotImplementedException( Assembler, GetServiceProvider(), "Clt: StackSizes>8 not supported", CurInstructionLabel, mMethodInfo, mCurrentOffset, NextInstructionLabel );
                throw new NotImplementedException("Cosmos.IL2CPU.x86->IL->Clt.cs->Error: StackSizes > 8 not supported");
                //return;
            }
            if( xStackItemSize > 4 )
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
                    XS.SSE2.MoveSD(XMM0, ESP, sourceIsIndirect: true);
                    // Increment ESP to get the value of the next double
                    XS.Add(ESP, 8);
                    XS.SSE2.MoveSD(XMM1, ESP, sourceIsIndirect: true);
                    XS.SSE2.CompareSD(XMM1, XMM0, comparision: LessThan);
                    XS.MoveD(EBX, XMM1);
                    XS.And(EBX, 1);
                    // We need to move the stack pointer of 4 Byte to "eat" the second double that is yet in the stack or we get a corrupted stack!
                    XS.Add(ESP, 4);
                    XS.Set(ESP, EBX, destinationIsIndirect: true);
                }
                else
                {
                    XS.Set(ESI, 1);
                    // esi = 1
                    XS.Xor(EDI, EDI);
                    // edi = 0
                    XS.Pop(EAX);
                    XS.Pop(EDX);
                    //value2: EDX:EAX
                    XS.Pop(EBX);
                    XS.Pop(ECX);
                    //value1: ECX:EBX
                    XS.Sub(EBX, EAX);
                    XS.SubWithCarry(ECX, EDX);
                    //result = value1 - value2

                    new ConditionalMove { Condition = ConditionalTestEnum.LessThan, DestinationReg = EDI, SourceReg = ESI };
                    XS.Push(EDI);
                }
            }
            else
            {
                if (xStackItemIsFloat)
                {
                    XS.SSE.MoveSS(XMM0, ESP, sourceIsIndirect: true);
                    XS.Add(ESP, 4);
                    XS.SSE.MoveSS(XMM1, ESP, sourceIsIndirect: true);
                    XS.SSE.CompareSS(XMM1, XMM0, comparision: LessThan);
                    XS.MoveD(EBX, XMM1);
                    XS.And(EBX, 1);
                    XS.Set(ESP, EBX, destinationIsIndirect: true);
                }
                else
                {
                    XS.Xor(EBX, EBX);
                    XS.Pop(ECX);
                    XS.Pop(EAX);
                    XS.Compare(EAX, ECX);
                    XS.SetByteOnCondition(ConditionalTestEnum.LessThan, BL);
                    XS.Push(EBX);
                }
            }
        }


        // using System;
        // using System.IO;
        //
        //
        // using CPUx86 = XSharp.Assembler.x86;
        // using CPU = XSharp.Assembler.x86;
        // using Cosmos.IL2CPU.X86;
        // using Cosmos.IL2CPU.X86;
        //
        // namespace Cosmos.IL2CPU.IL.X86 {
        // 	[XSharp.Assembler.OpCode(OpCodeEnum.Clt)]
        // 	public class Clt: Op {
        // 		private readonly string NextInstructionLabel;
        // 		private readonly string CurInstructionLabel;
        //         private uint mCurrentOffset;
        //         private MethodInformation mMethodInfo;
        //         public Clt(ILReader aReader, MethodInformation aMethodInfo)
        // 			: base(aReader, aMethodInfo) {
        // 			NextInstructionLabel = GetInstructionLabel(aReader.NextPosition);
        // 			CurInstructionLabel = GetInstructionLabel(aReader);
        //             mMethodInfo = aMethodInfo;
        //             mCurrentOffset = aReader.Position;
        // 		}
        // 		public override void DoAssemble() {

        // 		}
        // 	}
        // }

    }
}
