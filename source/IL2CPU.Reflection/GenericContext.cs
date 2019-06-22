using System;
using System.Collections.Generic;
using System.Linq;

namespace IL2CPU.Reflection
{
    internal class GenericContext
    {
        public static GenericContext Empty { get; } = new GenericContext(null, null);

        public IReadOnlyList<TypeInfo> TypeArguments { get; }
        public IReadOnlyList<TypeInfo> MethodArguments { get; }

        public GenericContext(
            IReadOnlyList<TypeInfo> typeArguments,
            IReadOnlyList<TypeInfo> methodArguments)
        {
            MethodArguments = methodArguments ?? Array.Empty<TypeInfo>();
            TypeArguments = typeArguments ?? Array.Empty<TypeInfo>();
        }

        public TypeInfo GetTypeArgument(int index)
        {
            if (index >= TypeArguments.Count)
            {
                return null;
            }

            return TypeArguments[index];
        }

        public TypeInfo GetMethodArgument(int index)
        {
            if (index >= MethodArguments.Count)
            {
                return null;
            }

            return MethodArguments[index];
        }

        public GenericContext WithTypeArguments(
            IReadOnlyList<TypeInfo> arguments)
        {
            var typeArguments = new List<TypeInfo>(TypeArguments.Count + arguments.Count);

            typeArguments.AddRange(TypeArguments);
            typeArguments.AddRange(arguments);

            return new GenericContext(typeArguments, MethodArguments);
        }

        public GenericContext WithMethodArguments(
            IReadOnlyList<TypeInfo> arguments)
        {
            var methodArguments = new List<TypeInfo>(TypeArguments.Count + arguments.Count);

            methodArguments.AddRange(MethodArguments);
            methodArguments.AddRange(arguments);

            return new GenericContext(TypeArguments, methodArguments);
        }

        public GenericContext WithTrimmedTypeArguments(int trimCount)
        {
            var count = TypeArguments.Count;

            if (trimCount == count)
            {
                return new GenericContext(null, MethodArguments);
            }

            var typeArguments = new List<TypeInfo>(count - trimCount);
            typeArguments.AddRange(TypeArguments.Take(count - trimCount));

            return new GenericContext(typeArguments, MethodArguments);
        }

        public GenericContext WithTrimmedMethodArguments(int trimCount)
        {
            var count = MethodArguments.Count;

            if (trimCount == count)
            {
                return new GenericContext(TypeArguments, null);
            }

            var methodArguments = new List<TypeInfo>(count - trimCount);
            methodArguments.AddRange(MethodArguments.Take(count - trimCount));

            return new GenericContext(TypeArguments, methodArguments);
        }
    }
}
