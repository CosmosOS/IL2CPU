using System.Linq;

namespace IL2CPU.Reflection
{
    public static class MethodInfoExtensions
    {
        public static MethodInfo GetBaseDefinition(
            this MethodInfo methodInfo)
        {
            if (methodInfo.IsNewSlot)
            {
                return methodInfo;
            }

            var type = methodInfo.DeclaringType;

            var methodDefinition = methodInfo.GetGenericMethodDefinition();
            var currentMethod = methodInfo;

            while (type != null)
            {
                var method = type.Methods.SingleOrDefault(m => m.Matches(methodDefinition));

                if (method != null)
                {
                    if (method.IsNewSlot)
                    {
                        return method;
                    }

                    currentMethod = method;
                }

                type = type.BaseType;
            }

            return currentMethod;
        }
    }
}
