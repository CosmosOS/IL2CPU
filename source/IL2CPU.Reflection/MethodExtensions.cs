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

        public static string ToFullStr(this MethodBase method)
        {
            var builder = new StringBuilder();
            if (method is MethodInfo normal)
            {
                builder.Append(ToFullStr(normal.ReturnType));
                builder.Append(" ");
            }
            if (method.DeclaringType != null)
            {
                builder.Append(ToFullStr(method.DeclaringType));
                builder.Append("::");
            }
            builder.Append(method.Name);
            builder.Append('(');
            var @params = String.Join(", ", method.GetParameters()
                .Select(p => ToFullStr(p.ParameterType) + " " + p.Name));
            builder.Append(@params);
            builder.Append(')');
            return builder.ToString();
        }

        public static string ToFullStr(this FieldInfo field)
        {
            var builder = new StringBuilder();
            builder.Append(ToFullStr(field.FieldType));
            builder.Append(" ");
            if (field.DeclaringType != null)
            {
                builder.Append(ToFullStr(field.DeclaringType));
                builder.Append("::");
            }
            builder.Append(field.Name);
            return builder.ToString();
        }

        public static string ToFullStr(this Type type)
        {
            var fullName = type.FullName;
            return fullName;
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
