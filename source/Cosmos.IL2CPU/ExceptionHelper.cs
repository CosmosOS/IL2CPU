using System;

using IL2CPU.API.Attribs;

namespace Cosmos.IL2CPU
{
    [ForceInclude]
    public static class ExceptionHelper
    {
#pragma warning disable CA2211 // Non-constant fields should not be visible
        public static Exception CurrentException;
#pragma warning restore CA2211 // Non-constant fields should not be visible

        public static void ThrowArgumentOutOfRange(string aError)
        {
            Console.WriteLine(aError);
            throw new ArgumentOutOfRangeException(aError);
        }

        public static void ThrowInvalidOperation(string aError)
        {
            Console.WriteLine(aError);
            throw new InvalidOperationException(aError);
        }

        public static void ThrowNotImplemented(string aError)
        {
            Console.WriteLine(aError);
            throw new NotImplementedException(aError);
        }

        public static void ThrowOverflow()
        {
            string xError = "Arithmetic operation gets an overflow!";
            Console.WriteLine(xError);
            throw new OverflowException(xError);
        }

        public static void ThrowNotFiniteNumberException(double offendingNumber)
        {
            throw new NotFiniteNumberException(offendingNumber);
        }

        public static void ThrowInvalidCastException() => throw new InvalidCastException();
    }
}
