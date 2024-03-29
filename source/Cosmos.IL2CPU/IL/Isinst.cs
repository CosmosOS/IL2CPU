using System;

using Cosmos.IL2CPU.ILOpCodes;
using XSharp;
using XSharp.Assembler;
using XSharp.Assembler.x86;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    /// <summary>
    /// Tests whether an object reference (type O) is an instance of a particular class.
    /// </summary>
    [OpCode(ILOpCode.Code.Isinst)]
    public class Isinst : ILOp
    {
        public Isinst(Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            OpType xType = (OpType)aOpCode;
            string xTypeID = GetTypeIDLabel(xType.Value);
            string xCurrentMethodLabel = GetLabel(aMethod, aOpCode);
            string xReturnNullLabel = xCurrentMethodLabel + "_ReturnNull";
            string xAfterIsInstanceCallLabel = xCurrentMethodLabel + "_After_IsInstance_Call";
            string xNextPositionLabel = GetLabel(aMethod, aOpCode.NextPosition);

            XS.Set(EAX, ESP, sourceIsIndirect: true, sourceDisplacement: 4);

            XS.Compare(EAX, 0);
            XS.Jump(ConditionalTestEnum.Zero, xReturnNullLabel);

            XS.Push(EAX, isIndirect: true);
            XS.Push(xTypeID, isIndirect: true);
            XS.Push(Convert.ToUInt32(xType.Value.IsInterface));

            Call.DoExecute(Assembler, aMethod, VTablesImplRefs.IsInstanceRef,
                aOpCode, xCurrentMethodLabel, xAfterIsInstanceCallLabel, DebugEnabled);

            XS.Label(xAfterIsInstanceCallLabel);

            XS.Pop(EAX);
            XS.Compare(EAX, 0);
            XS.Jump(ConditionalTestEnum.Equal, xReturnNullLabel);
            XS.Jump(xNextPositionLabel);

            XS.Label(xReturnNullLabel);

            XS.Add(ESP, 8);
            XS.Push(0);
            XS.Push(0);
        }
    }
}
