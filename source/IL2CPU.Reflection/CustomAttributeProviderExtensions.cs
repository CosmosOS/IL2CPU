using System;
using System.Collections.Generic;

using IL2CPU.Reflection.Types;

namespace IL2CPU.Reflection
{
    public static class CustomAttributeProviderExtensions
    {
        public static IReadOnlyList<CustomAttributeInfo> GetCustomAttributes(
            this ICustomAttributeProvider customAttributeProvider,
            DefinedType type)
        {
            if (customAttributeProvider == null)
            {
                throw new ArgumentNullException(nameof(customAttributeProvider));
            }

            var customAttributes = new List<CustomAttributeInfo>();

            foreach (var attribute in customAttributeProvider.CustomAttributes)
            {
                if (attribute.Constructor.DeclaringType == type)
                {
                    customAttributes.Add(attribute);
                }
            }

            return customAttributes;
        }
    }
}
