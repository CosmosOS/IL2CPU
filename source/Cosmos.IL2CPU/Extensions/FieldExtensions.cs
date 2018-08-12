using IL2CPU.Reflection;

namespace Cosmos.IL2CPU.Extensions
{
    public static class FieldExtensions
    {
        public static string GetFullName(this FieldInfo aField)
        {
            return aField.FieldType.GetFullName() + " " + aField.DeclaringType.GetFullName() + "." + aField.Name;
        }
    }
}
