using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using IL2CPU.API.Attribs;

namespace IL2CPU.API
{
    public static class LabelName
    {
        /// <summary>
        /// Cache for label names.
        /// </summary>
        private static Dictionary<MethodBase, string> LabelNamesCache = new Dictionary<MethodBase, string>();

        private static Dictionary<Assembly, int> AssemblyIds = new Dictionary<Assembly, int>();

        // All label naming code should be changed to use this class.

        // Label bases can be up to 200 chars. If larger they will be shortened with an included hash.
        // This leaves up to 56 chars for suffix information.

        // Suffixes are a series of tags and have their own prefixes to preserve backwards compat.
        // .GUID_xxxxxx
        // .IL_0000
        // .ASM_00 - future, currently is IL_0000 or IL_0000.00
        // Would be nice to combine IL and ASM into IL_0000_00, but the way we work with the assembler currently
        // we cant because the ASM labels are issued as local labels.
        //
        // - Methods use a variety of alphanumeric suffixes for support code.
        // - .00 - asm markers at beginning of method
        // - .0000.00 IL.ASM marker

        public static int LabelCount { get; private set; }
        // Max length of labels at 256. We use lower here so that we still have room for suffixes for IL positions, etc.
        const int MaxLengthWithoutSuffix = 200;

        public static string Get(MethodBase aMethod)
        {
            if (LabelNamesCache.TryGetValue(aMethod, out var result))
            {
                return result;
            }

            result = Final(GetFullName(aMethod));
            LabelNamesCache.Add(aMethod, result);
            return result;
        }

        public static string Get(string aMethodLabel, int aIlPos)
        {
            return aMethodLabel + ".IL_" + aIlPos.ToString("X4");
        }

        private const string IllegalIdentifierChars = "&.,+$<>{}-`\'/\\ ()[]*!=";
        // no array bracket, they need to replace, for unique names for used types in methods
        private static readonly Regex IllegalCharsReplace = new Regex(@"[&.,+$<>{}\-\`\\'/\\ \(\)\*!=]", RegexOptions.Compiled);

        public static string FilterStringForIncorrectChars(string aName)
        {
            string xTempResult = aName;
            foreach (char c in IllegalIdentifierChars)
            {
                xTempResult = xTempResult.Replace(c, '_');
            }
            return xTempResult;
        }

        public static string Final(string xName)
        {
            //var xSB = new StringBuilder(xName);

            // DataMember.FilterStringForIncorrectChars also does some filtering but replacing empties or non _ chars
            // causes issues with legacy hardcoded values. So we have a separate function.
            //
            // For logging possibilities, we generate fuller names, and then strip out spacing/characters.
            /*const string xIllegalChars = "&.,+$<>{}-`\'/\\ ()[]*!=_";
            foreach (char c in xIllegalChars) {
              xSB.Replace(c.ToString(), "");
            }*/
            xName = xName.Replace("[]", "array");
            xName = xName.Replace("<>", "compilergenerated");
            xName = xName.Replace("[,]", "array");
            xName = xName.Replace("*", "pointer");
            xName = xName.Replace("|", "sLine");

            xName = IllegalCharsReplace.Replace(xName, string.Empty);

            if (xName.Length > MaxLengthWithoutSuffix)
            {
                using (var xHash = MD5.Create())
                {
                    var xValue = xHash.ComputeHash(Encoding.GetEncoding(0).GetBytes(xName));
                    var xSB = new StringBuilder(xName);
                    // Keep length max same as before.
                    xSB.Length = MaxLengthWithoutSuffix - xValue.Length * 2;
                    foreach (var xByte in xValue)
                    {
                        xSB.Append(xByte.ToString("X2"));
                    }
                    xName = xSB.ToString();
                }
            }

            LabelCount++;
            return xName;
        }

        /// <summary>
        /// Get internal name for the type
        /// </summary>
        /// <param name="aType"></param>
        /// <param name="aAssemblyIncluded">If true, the assembly id is included</param>
        /// <returns></returns>
        public static string GetFullName(Type aType, bool aAssemblyIncluded = true)
        {
            if (aType.IsGenericParameter)
            {
                return aType.FullName;
            }
            StringBuilder stringBuilder = new StringBuilder(256);

            if (aAssemblyIncluded)
            {
                // Start the string with the id of the assembly
                Assembly assembly = aType.Assembly;
                if (!AssemblyIds.ContainsKey(assembly))
                {
                    AssemblyIds.Add(assembly, AssemblyIds.Count);
                }
                stringBuilder.Append("A" + AssemblyIds[assembly]);
            }

            if (aType.IsArray)
            {
                stringBuilder.Append(GetFullName(aType.GetElementType(), aAssemblyIncluded));
                stringBuilder.Append("[");
                int xRank = aType.GetArrayRank();
                while (xRank > 1)
                {
                    stringBuilder.Append(",");
                    xRank--;
                }
                stringBuilder.Append("]");
                return stringBuilder.ToString();
            }
            if (aType.IsByRef && aType.HasElementType)
            {
                return "&" + GetFullName(aType.GetElementType(), aAssemblyIncluded);
            }
            if (aType.IsGenericType && !aType.IsGenericTypeDefinition)
            {
                stringBuilder.Append(GetFullName(aType.GetGenericTypeDefinition(), aAssemblyIncluded));

                stringBuilder.Append("<");
                var xArgs = aType.GetGenericArguments();
                for (int i = 0; i < xArgs.Length - 1; i++)
                {
                    stringBuilder.Append(GetFullName(xArgs[i], aAssemblyIncluded));
                    stringBuilder.Append(", ");
                }
                stringBuilder.Append(GetFullName(xArgs.Last(), aAssemblyIncluded));
                stringBuilder.Append(">");
            }
            else
            {
                stringBuilder.Append(aType.FullName);
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Get the full name for the method
        /// </summary>
        /// <param name="aMethod"></param>
        /// <param name="aAssemblyIncluded">If true, id of assembly is included</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string GetFullName(MethodBase aMethod, bool aAssemblyIncluded = true)
        {
            if (aMethod == null)
            {
                throw new ArgumentNullException(nameof(aMethod));
            }
            StringBuilder xBuilder = new StringBuilder(256);
            string[] xParts = aMethod.ToString().Split(' ');
            MethodInfo xMethodInfo = aMethod as MethodInfo;
            if (xMethodInfo != null)
            {
                xBuilder.Append(GetFullName(xMethodInfo.ReturnType, aAssemblyIncluded));
            }
            else
            {
                ConstructorInfo xCtor = aMethod as ConstructorInfo;
                if (xCtor != null)
                {
                    xBuilder.Append(typeof(void).FullName);
                }
                else
                {
                    xBuilder.Append(xParts[0]);
                }
            }
            xBuilder.Append("  ");
            if (aMethod.DeclaringType != null)
            {
                xBuilder.Append(GetFullName(aMethod.DeclaringType, aAssemblyIncluded));
            }
            else
            {
                xBuilder.Append("dynamic_method");
            }
            xBuilder.Append(".");
            if (aMethod.IsGenericMethod && !aMethod.IsGenericMethodDefinition)
            {
                xBuilder.Append(xMethodInfo.GetGenericMethodDefinition().Name);

                var xGenArgs = aMethod.GetGenericArguments();
                if (xGenArgs.Length > 0)
                {
                    xBuilder.Append("<");
                    for (int i = 0; i < xGenArgs.Length - 1; i++)
                    {
                        xBuilder.Append(GetFullName(xGenArgs[i], aAssemblyIncluded));
                        xBuilder.Append(", ");
                    }
                    xBuilder.Append(GetFullName(xGenArgs.Last(), aAssemblyIncluded));
                    xBuilder.Append(">");
                }
            }
            else
            {
                xBuilder.Append(aMethod.Name);
            }
            xBuilder.Append("(");
            var xParams = aMethod.GetParameters();
            for (var i = 0; i < xParams.Length; i++)
            {
                if (i == 0 && xParams[i].Name == "aThis")
                {
                    continue;
                }
                xBuilder.Append(GetFullName(xParams[i].ParameterType, aAssemblyIncluded));
                if (i < xParams.Length - 1)
                {
                    xBuilder.Append(", ");
                }
            }
            xBuilder.Append(")");
            return xBuilder.ToString();
        }

        public static string GetFullName(FieldInfo aField)
        {
            return GetFullName(aField.FieldType, false) + " " + GetFullName(aField.DeclaringType, false) + "." + aField.Name;
        }

        /// <summary>
        /// Gets a label for the given static field
        /// </summary>
        /// <param name="aType"></param>
        /// <param name="aField"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">throws if its not static</exception>
        public static string GetStaticFieldName(Type aType, string aField)
        {
            return GetStaticFieldName(aType.GetField(aField));
        }

        /// <summary>
        /// Gets a label for the given static field
        /// </summary>
        /// <param name="aField"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">throws if its not static</exception>
        public static string GetStaticFieldName(FieldInfo aField)
        {
            if (!aField.IsStatic)
            {
                throw new NotSupportedException($"{aField.Name}: is not static");
            }

            return FilterStringForIncorrectChars(
                "static_field__" + GetFullName(aField.DeclaringType) + "." + aField.Name);
        }

        /// <summary>
        /// Gets a label for the given Manifest Resource Stream
        /// </summary>
        /// <param name="aType"></param>
        /// <param name="aField"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"> throws if does not have <see cref="T:ManifestResourceStreamAttribute"/> or is its not static or is not a <see cref="T:byte[]"/> </exception>
        public static string GetManifestResourceStreamName(Type aType, string aField)
        {
            return GetManifestResourceStreamName(aType.GetField(aField));
        }

        /// <summary>
        /// Gets a label for the given Manifest Resource Stream
        /// </summary>
        /// <param name="aField"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"> throws if does not have <see cref="T:ManifestResourceStreamAttribute"/> or is its not static or is not a <see cref="T:byte[]"/> </exception>
        public static string GetManifestResourceStreamName(FieldInfo aField)
        {
            if (
                    !aField.GetCustomAttributes<ManifestResourceStreamAttribute>(false).Any()
                )
            {
                throw new NotSupportedException($"{aField.Name}: is not static or not a byte array");
            }

            if (!aField.IsStatic || aField.FieldType != typeof(byte[]))
            {
                throw new NotSupportedException($"{aField.Name}: is not static or not a byte array");
            }

            return $"{GetStaticFieldName(aField)}__Contents";
        }

        public static string GetRandomLabel() => $"random_label__{Guid.NewGuid()}";

    }
}
