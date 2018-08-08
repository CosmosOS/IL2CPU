using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;

namespace IL2CPU.Reflection.Internal
{
    internal static class MethodSignatureExtensions
    {
        public static bool Matches<TType>(
            this MethodSignature<TType> signature,
            MethodSignature<TType> other) =>
            signature.Header == other.Header
            && signature.GenericParameterCount == other.GenericParameterCount
            && signature.ParameterTypes.SequenceEqual(other.ParameterTypes)
            && EqualityComparer<TType>.Default.Equals(signature.ReturnType, other.ReturnType);
    }
}
