using System;

namespace Cosmos.IL2CPU
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public sealed class OpCodeAttribute : Attribute
    {
        public ILOpCode.Code OpCode => opCode;

        private readonly ILOpCode.Code opCode;

        public OpCodeAttribute(ILOpCode.Code OpCode)
        {
            opCode = OpCode;
        }
    }
}
