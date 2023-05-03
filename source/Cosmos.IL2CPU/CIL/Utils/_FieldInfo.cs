using System;
using System.Diagnostics;
using System.Reflection;

namespace Cosmos.IL2CPU.CIL.Utils
{
    [DebuggerDisplay("Field = '{Id}', Offset = {Offset}, Size = {Size}")]
    public class _FieldInfo
    {
        public string Id { get; }

        public FieldInfo Field { get; set; }

        public Type DeclaringType { get; }
        public Type FieldType { get; set; }
        public uint Size { get; set; }
        public bool IsExternalValue { get; set; }
        public bool IsStatic { get; set; }

        public bool IsOffsetSet { get; private set; }

        /// <summary>
        /// Does NOT include any kind of method header!
        /// </summary>
        public uint Offset
        {
            get
            {
                if (!IsOffsetSet)
                {
                    throw new InvalidOperationException("Offset is being used, but hasnt been set yet!");
                }
                return mOffset;
            }
            set
            {
                IsOffsetSet = true;
                mOffset = value;
            }
        }

        private uint mOffset;

        public _FieldInfo(string aId, uint aSize, Type aDeclaringType, Type aFieldType)
        {
            Id = aId;
            DeclaringType = aDeclaringType;
            FieldType = aFieldType;
            Size = aSize;
        }
    }
}
