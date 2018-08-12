using IL2CPU.API;
using IL2CPU.Reflection;

namespace Cosmos.IL2CPU.Extensions
{
    public static class TypeExtensions
    {
        public static string GetFullName(this TypeInfo aType) => LabelName.GetFullName(aType);
    }
}
