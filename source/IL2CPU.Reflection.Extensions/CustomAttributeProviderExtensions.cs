using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace IL2CPU.Reflection
{
    public static class CustomAttributeProviderExtensions
    {
        public static T GetCustomAttribute<T>(this ICustomAttributeProvider customAttributeProvider) =>
            GetCustomAttributes<T>(customAttributeProvider).SingleOrDefault();

        public static T GetCustomAttribute<T>(this ICustomAttributeProvider customAttributeProvider, bool inherit) =>
            GetCustomAttributes<T>(customAttributeProvider, inherit).SingleOrDefault();

        public static IReadOnlyCollection<T> GetCustomAttributes<T>(
            this ICustomAttributeProvider customAttributeProvider) =>
            customAttributeProvider.GetCustomAttributes<T>(true);

        public static IReadOnlyCollection<T> GetCustomAttributes<T>(
            this ICustomAttributeProvider customAttributeProvider,
            bool inherit)
        {
            // todo: inherit

            var attributeType = customAttributeProvider.Module.MetadataContext.ImportType<T>();
            var customAttributeInfos = customAttributeProvider.CustomAttributes.Where(
                a => attributeType.IsAssignableFrom(a.AttributeType));

            return ResolveCustomAttributes<T>(customAttributeInfos);
        }

        private static IReadOnlyCollection<T> ResolveCustomAttributes<T>(
            IEnumerable<CustomAttributeInfo> customAttributeInfos)
        {
            var customAttributes = new List<T>();

            foreach (var customAttribute in customAttributeInfos)
            {
                var attribute = (T)Activator.CreateInstance(
                    Type.GetType(customAttribute.AttributeType.AssemblyQualifiedName),
                    customAttribute.FixedArguments.Select(a => EnsureCorrectValue(a.Type, a.Value)).ToArray());

                foreach (var fieldArgument in customAttribute.FieldArguments)
                {
                    typeof(T).GetTypeInfo().GetField(fieldArgument.Name)
                        .SetValue(attribute, EnsureCorrectValue(fieldArgument.Type, fieldArgument.Value));
                }

                foreach (var propertyArgument in customAttribute.PropertyArguments)
                {
                    typeof(T).GetTypeInfo().GetProperty(propertyArgument.Name)
                        .SetValue(attribute, EnsureCorrectValue(propertyArgument.Type, propertyArgument.Value));
                }

                customAttributes.Add(attribute);
            }

            return customAttributes;
        }

        private static object EnsureCorrectValue(TypeInfo type, object value)
        {
            var argumentType = Type.GetType(type.AssemblyQualifiedName);

            if (value is TypeInfo typeInfo)
            {
                return new CustomAttributeType(typeInfo);
            }
            else
            {
                if (argumentType != value.GetType())
                {
                    value = argumentType.IsEnum ? Enum.ToObject(argumentType, value)
                        : Convert.ChangeType(value, argumentType, CultureInfo.InvariantCulture);
                }

                return value;
            }
        }
    }
}
