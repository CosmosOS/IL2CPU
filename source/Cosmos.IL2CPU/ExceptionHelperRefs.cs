using System.Linq;

using IL2CPU.API.Attribs;
using IL2CPU.Reflection;
using static Cosmos.IL2CPU.ExceptionHelper;
using static Cosmos.IL2CPU.TypeRefHelper;

namespace Cosmos.IL2CPU
{
    [ForceInclude]
    public static class ExceptionHelperRefs
    {
        private static readonly TypeInfo ExceptionHelperTypeInfo = TypeOf(typeof(ExceptionHelper));

        public static readonly FieldInfo CurrentExceptionRef =
            ExceptionHelperTypeInfo.Fields.Single(f => f.Name == nameof(CurrentException));

        public static readonly MethodInfo ThrowArgumentOutOfRangeRef =
            ExceptionHelperTypeInfo.Methods.Single(m => m.Name == nameof(ThrowArgumentOutOfRange));

        public static readonly MethodInfo ThrowDivideByZeroExceptionRef =
            ExceptionHelperTypeInfo.Methods.Single(m => m.Name == nameof(ThrowDivideByZeroException));

        public static readonly MethodInfo ThrowInvalidOperationRef =
            ExceptionHelperTypeInfo.Methods.Single(m => m.Name == nameof(ThrowInvalidOperation));

        public static readonly MethodInfo ThrowNotImplementedRef =
            ExceptionHelperTypeInfo.Methods.Single(m => m.Name == nameof(ThrowNotImplemented));

        public static readonly MethodInfo ThrowOverflowRef =
            ExceptionHelperTypeInfo.Methods.Single(m => m.Name == nameof(ThrowOverflow));

        public static readonly MethodInfo ThrowInvalidCastExceptionRef =
            ExceptionHelperTypeInfo.Methods.Single(m => m.Name == nameof(ThrowInvalidCastException));

        public static readonly MethodInfo ThrowNotFiniteNumberExceptionRef =
            ExceptionHelperTypeInfo.Methods.Single(m => m.Name == nameof(ThrowNotFiniteNumberException));
    }
}
