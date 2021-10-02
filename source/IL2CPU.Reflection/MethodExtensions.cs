using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace IL2CPU.Reflection
{
    public static class MethodExtensions
    {
        private static readonly BindingFlags _all = BindingFlags.Instance | BindingFlags.Static |
                                                    BindingFlags.NonPublic | BindingFlags.Public;

        public static MethodInfo GetMyBaseDefinition(this MethodInfo method)
        {
            var declType = method.DeclaringType;
            return GetMyBaseDefinition(method, declType?.BaseType);
        }

        private static MethodInfo GetMyBaseDefinition(this MethodInfo method, Type type)
        {
            if (type == null)
            {
                return method;
            }
            if (!method.IsVirtual)
            {
                return method;
            }
            var maybe = type.GetMethods(_all)
                .Where(m => m.Name == method.Name && m.GetParameters().Length == method.GetParameters().Length)
                .ToArray();
            if (maybe.Length == 0)
            {
                return method;
            }
            if (maybe.Length == 1)
            {
                var @base = maybe.Single();
                return GetMyBaseDefinition(@base, type.BaseType);
            }
            throw new InvalidOperationException(method + " " + type);
        }

        public static string ToFullStr(this MethodBase method, bool withOwner = false)
        {
            var builder = new StringBuilder();
            if (method is MethodInfo method2)
            {
                builder.Append(method2.ReturnType.FullName);
                builder.Append(" ");
            }
            if (withOwner && method.DeclaringType != null)
            {
                builder.Append(method.DeclaringType.FullName);
                builder.Append("::");
            }
            builder.Append(method.Name);
            builder.Append('(');
            var @params = String.Join(", ", method.GetParameters()
                .Select(p => p.ParameterType.FullName + " " + p.Name));
            builder.Append(@params);
            builder.Append(')');
            return builder.ToString();
        }

        public static string ToFullStr(this FieldInfo field, bool withOwner = false)
        {
            var builder = new StringBuilder();
            builder.Append(field.FieldType.FullName);
            builder.Append(" ");
            if (withOwner && field.DeclaringType != null)
            {
                builder.Append(ToFullStr(field.DeclaringType));
                builder.Append("::");
            }
            builder.Append(field.Name);
            return builder.ToString();
        }

        private static string ToFullStr(Type type)
        {
            if (type.IsNested)
            {
                return ToFullStr(type.DeclaringType) + "+" + type.Name;
            }
            return type.FullName;
        }

        public static bool IsSame(this MethodBase first, MethodBase second)
        {
            if (first == null || second == null)
                return false;
            if (first == second)
                return true;
            if (first.Module == second.Module && first.MetadataToken == second.MetadataToken)
                return true;
            return false;
        }
    }
}
