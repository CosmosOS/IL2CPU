namespace IL2CPU.Reflection
{
    internal enum TypeKind
    {
        Array,
        ByReference,
        Defined,
        MethodPointer,
        Pinned,
#pragma warning disable CA1720 // Identifier contains type name
        Pointer,
#pragma warning restore CA1720 // Identifier contains type name
        SZArray
    }
}
