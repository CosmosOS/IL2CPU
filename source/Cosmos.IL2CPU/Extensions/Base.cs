using System;
using Cosmos.Core.DebugStub;
using Cosmos.IL2CPU.ILOpCodes;
using static IL2CPU.Reflection.TypeofExtensions;

namespace Cosmos.IL2CPU.Extensions
{
    internal static class Base
    {
        public static Type ExceptionHelper => Reload(typeof(ExceptionHelper));

        public static Type MulticastDelegate => Reload(typeof(MulticastDelegate));

        public static Type Exception => Reload(typeof(Exception));

        public static Type Array => Reload(typeof(Array));

        public static Type ValueType => Reload(typeof(ValueType));

        public static Type Enum => Reload(typeof(Enum));

        public static Type SbyteStar => Reload(typeof(sbyte*));

        public static Type CharStar => Reload(typeof(char*));

        public static Type CharArray => Reload(typeof(char[]));

        public static Type NullRef => Reload(typeof(NullRef));

        public static Type UintStar => Reload(typeof(uint*));

        public static Type VoidStar => Reload(typeof(void*));

        public static Type Nullable => Reload(typeof(Nullable<>));

        public static Type Box => Reload(typeof(Box<>));

        public static Type ReadOnlySpanChar => Reload(typeof(ReadOnlySpan<char>));

        public static Type RuntimeFieldHandle => Reload(typeof(RuntimeFieldHandle));

        public static Type RuntimeTypeHandle => Reload(typeof(RuntimeTypeHandle));

        public static Type VTablesImpl => Reload(typeof(VTablesImpl));

        public static Type RuntimeEngine => Reload(typeof(RuntimeEngine));

        public static Type VTable => Reload(typeof(VTable));

        public static Type ReferenceHelper => Reload(typeof(ReferenceHelper));
    }
}
