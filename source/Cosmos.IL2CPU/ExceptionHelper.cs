using System;
using System.Reflection;
using Cosmos.IL2CPU.Extensions;
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

        public static void ThrowDivideByZeroException() => throw new DivideByZeroException();

        public static void ThrowIndexOutOfRangeException() => throw new IndexOutOfRangeException();

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
            string xError = "Arithmetic operation or conversion causes an overflow!";
            throw new OverflowException(xError);
        }

        public static void ThrowNotFiniteNumberException(double offendingNumber)
        {
            throw new NotFiniteNumberException(offendingNumber);
        }
        public static void ThrowInvalidCastException() => throw new InvalidCastException();
    }

    [ForceInclude]
    public static class ExceptionHelperRefs
    {
        public static readonly FieldInfo CurrentExceptionRef = Base.ExceptionHelper.GetField("CurrentException");

        public static readonly MethodInfo ThrowOverflowExceptionRef =
            Base.ExceptionHelper.GetMethod(nameof(ExceptionHelper.ThrowOverflow));

        public static readonly MethodInfo ThrowDivideByZeroExceptionRef =
            Base.ExceptionHelper.GetMethod(nameof(ExceptionHelper.ThrowDivideByZeroException));

        public static readonly MethodInfo ThrowInvalidCastExceptionRef =
            Base.ExceptionHelper.GetMethod(nameof(ExceptionHelper.ThrowInvalidCastException));

        public static readonly MethodInfo ThrowNotFiniteNumberExceptionRef =
            Base.ExceptionHelper.GetMethod(nameof(ExceptionHelper.ThrowNotFiniteNumberException));

        public static readonly MethodInfo ThrowIndexOutOfRangeException =
            Base.ExceptionHelper.GetMethod(nameof(ExceptionHelper.ThrowIndexOutOfRangeException));
    }
}
