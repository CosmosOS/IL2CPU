using System;
using static IL2CPU.Reflection.TypeofExtensions;

namespace IL2CPU.Reflection
{
    public class BaseTypeSystem : IBaseTypeSystem
    {
        public static readonly IBaseTypeSystem BaseTypes = new BaseTypeSystem();

        public Type Void => Reload(typeof(void));

        public Type Boolean => Reload<bool>();

        public Type Char => Reload<char>();

        public Type SByte => Reload<sbyte>();

        public Type Byte => Reload<byte>();

        public Type Int16 => Reload<short>();

        public Type UInt16 => Reload<ushort>();

        public Type Int32 => Reload<int>();

        public Type UInt32 => Reload<uint>();

        public Type Int64 => Reload<long>();

        public Type UInt64 => Reload<ulong>();

        public Type Single => Reload<float>();

        public Type Double => Reload<double>();

        public Type String => Reload<string>();

        public Type TypedReference => Reload<Type>();

        public Type IntPtr => Reload<IntPtr>();

        public Type UIntPtr => Reload<UIntPtr>();

        public Type Object => Reload<object>();
    }
}
