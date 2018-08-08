namespace IL2CPU.Reflection
{
    public static class AssemblyInfoExtensions
    {
        public static TypeInfo GetType(
            this AssemblyInfo assembly,
            string fullName)
        {
            foreach (var type in assembly.ExportedTypes)
            {
                if (type.FullName == fullName)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
