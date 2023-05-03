using System.Reflection;
using IL2CPU.API;

namespace Cosmos.IL2CPU.CIL.Utils.Extensions
{
    public static class FieldExtensions
    {
        public static string GetFullName(this FieldInfo aField)
        {
            return LabelName.GetFullName(aField);
        }
    }
}
