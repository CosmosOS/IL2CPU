using System;
using System.Collections.Generic;

using XSharp;
using XSharp.Assembler;
using XSharp.Assembler.x86;
using XSharp.Assembler.x86.SSE;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Beq)]
    [OpCode(ILOpCode.Code.Bge)]
    [OpCode(ILOpCode.Code.Bgt)]
    [OpCode(ILOpCode.Code.Ble)]
    [OpCode(ILOpCode.Code.Blt)]
    [OpCode(ILOpCode.Code.Bne_Un)]
    [OpCode(ILOpCode.Code.Bge_Un)]
    [OpCode(ILOpCode.Code.Bgt_Un)]
    [OpCode(ILOpCode.Code.Ble_Un)]
    [OpCode(ILOpCode.Code.Blt_Un)]
    [OpCode(ILOpCode.Code.Brfalse)]
    [OpCode(ILOpCode.Code.Brtrue)]
    public class Branch : ILOp
    {
        private static readonly Dictionary<ILOpCode.Code, ConditionalTestEnum> TestOPs = new Dictionary<ILOpCode.Code, ConditionalTestEnum>()
        {
            [ILOpCode.Code.Beq] = ConditionalTestEnum.Equal,
            [ILOpCode.Code.Bge] = ConditionalTestEnum.GreaterThanOrEqualTo,
            [ILOpCode.Code.Bgt] = ConditionalTestEnum.GreaterThan,
            [ILOpCode.Code.Ble] = ConditionalTestEnum.LessThanOrEqualTo,
            [ILOpCode.Code.Blt] = ConditionalTestEnum.LessThan,
            [ILOpCode.Code.Bne_Un] = ConditionalTestEnum.NotEqual,
            [ILOpCode.Code.Bge_Un] = ConditionalTestEnum.AboveOrEqual,
            [ILOpCode.Code.Bgt_Un] = ConditionalTestEnum.Above,
            [ILOpCode.Code.Ble_Un] = ConditionalTestEnum.BelowOrEqual,
            [ILOpCode.Code.Blt_Un] = ConditionalTestEnum.Below
        };

        private static readonly Dictionary<ILOpCode.Code, ComparePseudoOpcodes> SseCompareOPs = new Dictionary<ILOpCode.Code, ComparePseudoOpcodes>()
        {
            [ILOpCode.Code.Beq] = ComparePseudoOpcodes.Equal,
            [ILOpCode.Code.Bge] = ComparePseudoOpcodes.NotLessThan,
            [ILOpCode.Code.Bgt] = ComparePseudoOpcodes.NotLessThanOrEqualTo,
            [ILOpCode.Code.Ble] = ComparePseudoOpcodes.LessThanOrEqualTo,
            [ILOpCode.Code.Blt] = ComparePseudoOpcodes.LessThan,
            [ILOpCode.Code.Bne_Un] = ComparePseudoOpcodes.NotEqual,
            [ILOpCode.Code.Bge_Un] = ComparePseudoOpcodes.NotLessThan,
            [ILOpCode.Code.Bgt_Un] = ComparePseudoOpcodes.NotLessThanOrEqualTo,
            [ILOpCode.Code.Ble_Un] = ComparePseudoOpcodes.LessThanOrEqualTo,
            [ILOpCode.Code.Blt_Un] = ComparePseudoOpcodes.LessThan
        };

        public Branch(Assembler aAsmblr)
          : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xOp = aOpCode.OpCode;
            var xBranchLabel = AppAssembler.TmpBranchLabel(aMethod, aOpCode);

            var xStackType = aOpCode.StackPopTypes[0];
            var xSize = SizeOfType(xStackType);
            var xIsFloat = TypeIsFloat(xStackType);

            if (xOp == ILOpCode.Code.Brtrue
                || xOp == ILOpCode.Code.Brfalse)
            {
                if (xIsFloat)
                {
                    throw new NotSupportedException();
                }

                if (xSize <= 4)
                {
                    XS.Pop(EAX);
                    XS.Compare(EAX, 0);
                    XS.Jump(xOp == ILOpCode.Code.Brtrue ? ConditionalTestEnum.NotEqual : ConditionalTestEnum.Equal, xBranchLabel);
                }
                else if (xSize <= 8)
                {
                    XS.Pop(EAX);
                    XS.Pop(EBX);

                    if (xOp == ILOpCode.Code.Brtrue)
                    {
                        XS.Compare(EAX, 0);
                        XS.Jump(ConditionalTestEnum.NotEqual, xBranchLabel);

                        XS.Compare(EBX, 0);
                        XS.Jump(ConditionalTestEnum.NotEqual, xBranchLabel);
                    }
                    else
                    {
                        var xEndLabel = GetLabel(aMethod, aOpCode) + ".End";

                        XS.Compare(EAX, 0);
                        XS.Jump(ConditionalTestEnum.NotEqual, xEndLabel);

                        XS.Compare(EBX, 0);
                        XS.Jump(ConditionalTestEnum.Equal, xBranchLabel);

                        XS.Label(xEndLabel);
                    }
                }
                else if (xSize > 8)
                {
                    throw new NotImplementedException("Cosmos.IL2CPU.x86->IL->Branch.cs->Error: StackSize > 8 not supported");
                }

                return;
            }

            if (xIsFloat)
            {
                var xTestOp = SseCompareOPs[xOp];
                var xIsUnordered = xOp == ILOpCode.Code.Bge_Un
                                || xOp == ILOpCode.Code.Bgt_Un
                                || xOp == ILOpCode.Code.Ble_Un
                                || xOp == ILOpCode.Code.Blt_Un
                                || xOp == ILOpCode.Code.Bne_Un;

                if (xSize <= 4)
                {
                    XS.SSE.MoveSS(XMM0, ESP, sourceIsIndirect: true);
                    XS.Add(ESP, 4);
                    XS.SSE.MoveSS(XMM1, ESP, sourceIsIndirect: true);
                    XS.Add(ESP, 4);

                    if (xIsUnordered)
                    {
                        XS.SSE.MoveSS(XMM2, XMM1);
                        XS.SSE.CompareSS(XMM2, XMM0, ComparePseudoOpcodes.Unordered);
                        XS.MoveD(EAX, XMM2);
                        XS.Compare(EAX, 0);
                        XS.Jump(ConditionalTestEnum.NotEqual, xBranchLabel);
                    }

                    XS.SSE.CompareSS(XMM1, XMM0, xTestOp);
                    XS.MoveD(EAX, XMM1);
                    XS.Compare(EAX, 0);
                    XS.Jump(ConditionalTestEnum.NotEqual, xBranchLabel);
                }
                else if (xSize <= 8)
                {
                    XS.SSE2.MoveSD(XMM0, ESP, sourceIsIndirect: true);
                    XS.Add(ESP, 8);
                    XS.SSE2.MoveSD(XMM1, ESP, sourceIsIndirect: true);
                    XS.Add(ESP, 8);

                    if (xIsUnordered)
                    {
                        XS.SSE2.MoveSD(XMM2, XMM1);
                        XS.SSE2.CompareSD(XMM2, XMM0, ComparePseudoOpcodes.Unordered);
                        XS.MoveD(EAX, XMM2);
                        XS.Compare(EAX, 0);
                        XS.Jump(ConditionalTestEnum.NotEqual, xBranchLabel);
                    }

                    XS.SSE2.CompareSD(XMM1, XMM0, xTestOp);
                    XS.MoveD(EAX, XMM1);
                    XS.Compare(EAX, 0);
                    XS.Jump(ConditionalTestEnum.NotEqual, xBranchLabel);
                }
            }
            else
            {
                var xTestOp = TestOPs[xOp];

                if (xSize <= 4)
                {
                    XS.Pop(EAX);
                    XS.Pop(EBX);
                    XS.Compare(EBX, EAX);
                    XS.Jump(xTestOp, xBranchLabel);
                }
                else if (xSize <= 8)
                {
                    var xEndLabel = GetLabel(aMethod, aOpCode) + ".End";

                    XS.Pop(EAX);
                    XS.Pop(EDX);

                    XS.Pop(EBX);
                    XS.Pop(ECX);

                    switch (xOp)
                    {
                        case ILOpCode.Code.Beq:
                        case ILOpCode.Code.Bne_Un:
                            XS.Compare(ECX, EDX);
                            XS.Jump(ConditionalTestEnum.NotEqual, xOp == ILOpCode.Code.Beq ? xEndLabel : xBranchLabel);
                            XS.Compare(EBX, EAX);
                            XS.Jump(xTestOp, xBranchLabel);

                            break;
                        case ILOpCode.Code.Bge:
                        case ILOpCode.Code.Bgt:
                            XS.Compare(ECX, EDX);
                            XS.Jump(ConditionalTestEnum.GreaterThan, xBranchLabel);
                            XS.Jump(ConditionalTestEnum.NotEqual, xEndLabel);
                            XS.Compare(EBX, EAX);
                            XS.Jump(xTestOp, xBranchLabel);

                            break;
                        case ILOpCode.Code.Ble:
                        case ILOpCode.Code.Blt:
                            XS.Compare(ECX, EDX);
                            XS.Jump(ConditionalTestEnum.LessThan, xBranchLabel);
                            XS.Jump(ConditionalTestEnum.NotEqual, xEndLabel);
                            XS.Compare(EBX, EAX);
                            XS.Jump(xTestOp, xBranchLabel);

                            break;
                        case ILOpCode.Code.Bge_Un:
                        case ILOpCode.Code.Bgt_Un:
                            XS.Compare(ECX, EDX);
                            XS.Jump(ConditionalTestEnum.Above, xBranchLabel);
                            XS.Jump(ConditionalTestEnum.NotEqual, xEndLabel);
                            XS.Compare(EBX, EAX);
                            XS.Jump(xTestOp, xBranchLabel);

                            break;
                        case ILOpCode.Code.Ble_Un:
                        case ILOpCode.Code.Blt_Un:
                            XS.Compare(ECX, EDX);
                            XS.Jump(ConditionalTestEnum.Below, xBranchLabel);
                            XS.Jump(ConditionalTestEnum.NotEqual, xEndLabel);
                            XS.Compare(EBX, EAX);
                            XS.Jump(xTestOp, xBranchLabel);

                            break;
                    }

                    XS.Label(xEndLabel);
                }
            }

            if (xSize > 8)
            {
                throw new NotImplementedException("Cosmos.IL2CPU.x86->IL->Branch.cs->Error: StackSize > 8 not supported");
            }
        }
    }
}
