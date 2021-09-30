using System;
using static IL2CPU.Reflection.TypeofExtensions;

namespace Cosmos.IL2CPU.Extensions
{
    internal static class Base
    {
        public static Type ExceptionHelper => Reload(typeof(ExceptionHelper));

        public static Type MulticastDelegate => Reload(typeof(MulticastDelegate));
    }
}
