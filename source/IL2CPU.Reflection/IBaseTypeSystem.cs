using System;

namespace IL2CPU.Reflection
{
    public interface IBaseTypeSystem
    {
        Type Void { get; }

        Type Boolean { get; }

        Type Char { get; }

        Type SByte { get; }

        Type Byte { get; }

        Type Int16 { get; }

        Type UInt16 { get; }

        Type Int32 { get; }

        Type UInt32 { get; }

        Type Int64 { get; }

        Type UInt64 { get; }

        Type Single { get; }

        Type Double { get; }

        Type String { get; }

        Type TypedReference { get; }

        Type IntPtr { get; }

        Type UIntPtr { get; }

        Type Object { get; }
    }
}
