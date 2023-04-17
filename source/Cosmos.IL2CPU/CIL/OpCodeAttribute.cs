using System;

namespace Cosmos.IL2CPU.CIL
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public sealed class OpCodeAttribute : Attribute
    {
        public ILOpCode.Code OpCode { get; }

        public OpCodeAttribute(ILOpCode.Code aOpCode)
        {
            OpCode = aOpCode;
        }
    }
}