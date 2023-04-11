using System;

namespace Cosmos.IL2CPU
{
    public class PlugInfo
    {
        /// <summary>
        /// The index in mMethodsToProcess of the plug method.
        /// </summary>
        public uint TargetUID { get; }

        public Type PlugMethodAssembler { get; }

        public PlugInfo(uint aTargetUID, Type aPlugMethodAssembler)
        {
            TargetUID = aTargetUID;
            PlugMethodAssembler = aPlugMethodAssembler;
        }
    }
}