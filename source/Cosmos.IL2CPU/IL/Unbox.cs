using System;

using IL2CPU.API;
using Cosmos.IL2CPU.ILOpCodes;

using XSharp;
using XSharp.Assembler.x86;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
    [OpCode(ILOpCode.Code.Unbox)]
    public class Unbox : ILOp
    {
        public Unbox(XSharp.Assembler.Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xType = ((OpType)aOpCode).Value;
            var xOpLabel = GetLabel(aMethod, aOpCode);
            var xAfterIsInstanceCallLabel = xOpLabel + ".AfterIsInstanceCall";
            var xAfterInstanceCheckLabel = xOpLabel + ".AfterInstanceCheck";

            XS.Add(RSP, 4);

            var xIsNullable = xType.IsGenericType && xType.GetGenericTypeDefinition() == typeof(Nullable<>);

            if (xIsNullable)
            {
                xType = xType.GenericTypeArguments[0];

                XS.Compare(RSP, 0, destinationIsIndirect: true);
                XS.Jump(ConditionalTestEnum.Equal, xAfterInstanceCheckLabel);
            }
            else
            {
                DoNullReferenceCheck(Assembler, DebugEnabled, 0);
            }

            var xTypeId = GetTypeIDLabel(xType);

            XS.Set(RAX, RSP, sourceIsIndirect: true);
            XS.Push(RAX, isIndirect: true);
            XS.Push(xTypeId, isIndirect: true);
            XS.Push(Convert.ToUInt32(xType.IsInterface));
            Call.DoExecute(Assembler, aMethod, VTablesImplRefs.IsInstanceRef, aOpCode, GetLabel(aMethod, aOpCode), xAfterIsInstanceCallLabel, DebugEnabled);

            XS.Label(xAfterIsInstanceCallLabel);

            XS.Pop(RAX);

            XS.Compare(RAX, 0);
            XS.Jump(ConditionalTestEnum.NotEqual, xAfterInstanceCheckLabel);

            XS.Call(LabelName.Get(ExceptionHelperRefs.ThrowInvalidCastExceptionRef));

            XS.Label(xAfterInstanceCheckLabel);

            if (xIsNullable)
            {
                // from ECMA 335:
                //
                // [Note: Typically, unbox simply computes the address of the value type that is already present
                // inside of the boxed object. This approach is not possible when unboxing nullable value types.
                // Because Nullable<T> values are converted to boxed Ts during the box operation, an
                // implementation often must manufacture a new Nullable<T> on the heap and compute the address
                // to the newly allocated object. end note]

                // todo:
                // - manufacture new Nullable<T>
                // - hasValue = (obj != null)
                // - if (obj == null) value = default(T)
                // - else value = obj data

                throw new NotSupportedException($"OpCode data: xType={xType}, xOpLabel={xOpLabel}, xIsNullable=true");

            }

            // the result is a managed pointer, it should be tracked by GC
            XS.Add(RSP, ObjectUtils.FieldDataOffset, destinationIsIndirect: true);
        }
    }
}
