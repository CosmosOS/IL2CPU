using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace IL2CPU.Reflection
{
    internal class MultipleReturnTypeBinder : Binder
    {
        private readonly MethodSignature<Type> _sig;
        private readonly SomeGenerics _ctx;

        public MultipleReturnTypeBinder(MethodSignature<Type> sig, SomeGenerics ctx)
        {
            _sig = sig;
            _ctx = ctx;
        }

        public override FieldInfo BindToField(BindingFlags bindingAttr, FieldInfo[] match,
            object value, CultureInfo culture) => throw new NotImplementedException();

        public override MethodBase BindToMethod(BindingFlags bindingAttr, MethodBase[] match,
            ref object[] args, ParameterModifier[] modifiers, CultureInfo culture,
            string[] names, out object state) => throw new NotImplementedException();

        public override object ChangeType(object value, Type type, CultureInfo culture)
            => throw new NotImplementedException();

        public override void ReorderArgumentArray(ref object[] args, object state)
            => throw new NotImplementedException();

        public override MethodBase SelectMethod(BindingFlags bindingAttr, MethodBase[] match,
            Type[] types, ParameterModifier[] modifiers)
        {
            var maybe = match
                .Where(m => CompareParams(m, types))
                .OfType<MethodInfo>()
                .Where(m => m.ReturnType == _sig.ReturnType)
                .Where(m => m.ContainsGenericParameters == _sig.Header.IsGeneric
                            && m.GetGenericArguments().Length == _sig.GenericParameterCount)
                .ToArray();
            if (maybe.Length == 1)
                return maybe[0];
            return null;
        }

        private static bool CompareParams(MethodBase method, Type[] types)
        {
            var pars = method.GetParameters();
            if (pars.Length != types.Length)
                return false;
            for (var i = 0; i < types.Length; i++)
            {
                var par = pars[i];
                var type = types[i];
                if (par.ParameterType != type)
                    return false;
            }
            return true;
        }

        public override PropertyInfo SelectProperty(BindingFlags bindingAttr, PropertyInfo[] match,
            Type returnType, Type[] indexes, ParameterModifier[] modifiers)
            => throw new NotImplementedException();
    }
}
