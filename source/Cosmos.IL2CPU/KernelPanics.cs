using System.Diagnostics.CodeAnalysis;

namespace Cosmos.IL2CPU
{
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Scope = "member")]
    public static class KernelPanics
    {
        public const uint VMT_MethodNotFound = 0x1;
        public const uint VMT_MethodFoundButAddressInvalid = 0x2;
        public const uint VMT_MethodAddressesNull = 0x3;
        public const uint VMT_MethodIndexesNull = 0x4;
        public const uint VMT_TypeIdInvalid = 0x5;
    }
}
