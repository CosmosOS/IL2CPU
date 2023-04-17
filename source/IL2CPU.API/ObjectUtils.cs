namespace IL2CPU.API
{
    public static class ObjectUtils
    {
        /// <summary>
        ///		<para>
        ///			The object first stores any metadata involved. (Most likely containing a reference to the
        ///			object type). This is the number of bytes.
        ///		</para>
        ///		<para>
        ///			The first 4 bytes are the reference to the type information of the instance,
        ///         the second 4 bytes are the <see cref="InstanceTypeEnum"/> value.
        ///         For arrays, there are 4 following bytes containing the element count,
        ///         for objects, the amount of reference fields.
        ///         For arrays, next 4 bytes containing the element size.
        ///		</para>
        /// </summary>
        public const int FieldDataOffset = 12;

        public enum InstanceTypeEnum : uint
        {
            NormalObject = 1,

            Array = 2,

            BoxedValueType = 3,

            StaticEmbeddedObject = 0x80000001,

            StaticEmbeddedArray = 0x80000002
        }
    }
}
