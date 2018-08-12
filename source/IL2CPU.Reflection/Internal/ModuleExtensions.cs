using System.Collections.Generic;
using System.Reflection.Metadata;

namespace IL2CPU.Reflection.Internal
{
    internal static class ModuleExtensions
    {
        public static IReadOnlyCollection<CustomAttributeInfo> ResolveCustomAttributes(
            this ModuleInfo module, CustomAttributeHandleCollection handles)
        {
            var customAttributes = new List<CustomAttributeInfo>(handles.Count);

            foreach (var handle in handles)
            {
                customAttributes.Add(
                    new CustomAttributeInfo(module, module.MetadataReader.GetCustomAttribute(handle)));
            }

            return customAttributes;
        }
    }
}
