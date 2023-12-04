using System;
using XSharp;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Add)]
    public class Add : ILOp
    {
        public Add(XSharp.Assembler.Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xType = aOpCode.StackPopTypes[0];
            var xSize = SizeOfType(xType);
            var xIsFloat = TypeIsFloat(xType);
            DoExecute(xSize, xIsFloat);
        }

        public static void DoExecute(uint xSize, bool xIsFloat)
        {
            if (xSize > 8)
            {
                //EmitNotImplementedException( Assembler, aServiceProvider, "Size '" + xSize.Size + "' not supported (add)", aCurrentLabel, aCurrentMethodInfo, aCurrentOffset, aNextLabel );
                throw new NotImplementedException("Cosmos.IL2CPU.x86->IL->Add.cs->Error: StackSize > 8 not supported");
            }
            else
            {
                if (xSize > 4)
                {
                    if (xIsFloat) // double
                    {

                        // Please note that SSE supports double operations only from version 2
                        XS.SSE2.MoveSD(XMM0, RSP, sourceIsIndirect: true);
                        // Move the stack of 8 bytes to get the second double
                        XS.Add(RSP, 8);
                        XS.SSE2.MoveSD(XMM1, RSP, sourceIsIndirect: true);
                        XS.SSE2.AddSD(XMM1, XMM0);
                        XS.SSE2.MoveSD(RSP, XMM1, destinationIsIndirect: true);
                    }
                    else // long
                    {
                        XS.Pop(RDX); // low part
                        XS.Pop(RAX); // high part
                        XS.Add(RSP, RDX, destinationIsIndirect: true);
                        XS.AddWithCarry(RSP, RAX, destinationDisplacement: 4);
                    }
                }
                else
                {
                    if (xIsFloat) //float
                    {
                        XS.SSE.MoveSS(XMM0, RSP, sourceIsIndirect: true);
                        XS.Add(RSP, 4);
                        XS.SSE.MoveSS(XMM1, RSP, sourceIsIndirect: true);
                        XS.SSE.AddSS(XMM1, XMM0);
                        XS.SSE.MoveSS(RSP, XMM1, destinationIsIndirect: true);
                    }
                    else //integer
                    {
                        XS.Pop(RAX);
                        XS.Add(RSP, RAX, destinationIsIndirect: true);
                    }
                }
            }
        }
    }
}
