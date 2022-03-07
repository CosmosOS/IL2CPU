using System;
using System.Reflection;

using IL2CPU.API;
using Cosmos.IL2CPU.ILOpCodes;

using XSharp;
using XSharp.Assembler.x86;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Castclass)]
    public class Castclass : ILOp
    {
        public Castclass(XSharp.Assembler.Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xType = (OpType)aOpCode;
            var xTypeID = GetTypeIDLabel(xType.Value);

            var xCurrentMethodLabel = GetLabel(aMethod, aOpCode);
            var xAfterIsInstanceCallLabel = xCurrentMethodLabel + "_After_IsInstance_Call";
            var xInvalidCastLabel = xCurrentMethodLabel + "_InvalidCast";
            var xNextPositionLabel = GetLabel(aMethod, aOpCode.NextPosition);

            XS.Set(EAX, ESP, sourceDisplacement: 4);

            XS.Compare(EAX, 0);
            XS.Jump(ConditionalTestEnum.Zero, xNextPositionLabel);
            XS.Push(EAX, isIndirect: true);
            XS.Push(xTypeID, isIndirect: true);
            XS.Push(Convert.ToUInt32(xType.Value.IsInterface));

            MethodBase xMethodIsInstance = VTablesImplRefs.IsInstanceRef;

            Call.DoExecute(Assembler, aMethod, xMethodIsInstance, aOpCode, xCurrentMethodLabel, xAfterIsInstanceCallLabel, DebugEnabled);

            XS.Label(xAfterIsInstanceCallLabel);

            XS.Pop(EAX);

            XS.Compare(EAX, 0);
            XS.Jump(ConditionalTestEnum.Equal, xInvalidCastLabel);

            XS.Jump(xNextPositionLabel);

            XS.Label(xInvalidCastLabel);
            XS.Call(LabelName.Get(ExceptionHelperRefs.ThrowInvalidCastExceptionRef));
        }
    }
}
