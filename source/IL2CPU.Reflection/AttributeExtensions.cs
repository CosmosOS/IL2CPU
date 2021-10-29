using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IL2CPU.Reflection
{
    public static class AttributeExtensions
    {
        public static T FetchCustomAttribute<T>(this ICustomAttributeProvider c, bool inherit = true)
            where T : Attribute
        {
            var attrs = FetchCustomAttributes<T>(c, inherit);
            return attrs.SingleOrDefault();
        }

        public static IEnumerable<T> FetchCustomAttributes<T>(this ICustomAttributeProvider c, bool inherit = true)
            where T : Attribute
        {
            var type = typeof(T);
            return FetchCustomAttributes(c, type, inherit).Select(o => (T)o);
        }

        private static IEnumerable<object> FetchCustomAttributes(this ICustomAttributeProvider c, Type type, bool inherit)
        {
            var data = c is ParameterInfo hp ? hp.GetCustomAttributesData()
                : c is MemberInfo hm ? hm.GetCustomAttributesData()
                : throw new InvalidOperationException(c.GetType().FullName);
            var possible = data.Where(d => d.AttributeType.FullName == type.FullName);
            var items = possible.Select(p => LoadCustomAttribute(type, p)).ToArray();
            if (inherit)
            {
                var usage = type.GetCustomAttribute<AttributeUsageAttribute>();
                if (usage.Inherited && (items.Length == 0 || usage.AllowMultiple))
                {
                    if (c is Type mType)
                    {
                        if (mType.BaseType != null)
                            items = items.Concat(FetchCustomAttributes(mType.BaseType, type, true)).ToArray();
                    }
                    else if (c is ParameterInfo mParm)
                    {
                        if (mParm.Member is MethodInfo mMInfo)
                        {
                            var baseMethod = mMInfo.FetchBaseDefinition();
                            if (baseMethod != mMInfo)
                            {
                                var baseParam = baseMethod.GetParameters()
                                    .FirstOrDefault(p => p.Name == mParm.Name);
                                if (baseParam != null)
                                    items = items.Concat(FetchCustomAttributes(baseParam, type, true)).ToArray();
                            }
                        }
                    }
                    else if (c is MethodBase mMethod)
                    {
                        if (mMethod is MethodInfo mMInfo)
                        {
                            var baseMethod = mMInfo.FetchBaseDefinition();
                            if (baseMethod != mMInfo)
                                items = items.Concat(FetchCustomAttributes(baseMethod, type, true)).ToArray();
                        }
                    }
                    else if (c is FieldInfo)
                    {
                        // No inheritance!
                    }
                    else
                    {
                        throw new InvalidOperationException(c + " ?");
                    }
                }
            }
            return items;
        }

        private static object LoadCustomAttribute(Type type, CustomAttributeData data)
        {
            var args = data.ConstructorArguments.Select(ExtractValue).ToArray();
            var obj = Activator.CreateInstance(type, args);
            if (data.NamedArguments != null)
                foreach (var arg in data.NamedArguments)
                {
                    var name = arg.MemberName;
                    var value = arg.TypedValue.Value;
                    if (arg.IsField)
                    {
                        var field = type.GetField(name);
                        field.SetValue(obj, value);
                        continue;
                    }
                    var prop = type.GetProperty(name);
                    prop.SetValue(obj, value);
                }
            return obj;
        }

        private static object ExtractValue(CustomAttributeTypedArgument c)
        {
            if (c.ArgumentType.IsEnum)
            {
                var enumType = ToRuntimeType(c.ArgumentType);
                var enumVal = Enum.Parse(enumType, c.Value.ToString());
                return enumVal;
            }
            return c.Value;
        }

        private static Type ToRuntimeType(Type argType)
        {
            var type = Type.GetType(argType.FullName + ", " + argType.Assembly.GetName().Name);
            return type;
        }
    }
}
