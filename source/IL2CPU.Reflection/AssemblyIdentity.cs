using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;

namespace IL2CPU.Reflection
{
    public class AssemblyIdentity
    {
        private const string NeutralCulture = "neutral";

        private static readonly Version ZeroVersion = new Version(0, 0, 0, 0);

        public string Name { get; }
        public Version Version { get; }
        public string Culture { get; }
        public ImmutableArray<byte> PublicKeyToken { get; }

        public string FullName => $"{Name}, Version={Version}, Culture={Culture ?? NeutralCulture}, PublicKeyToken={GetTokenAsString()}";
        
        public AssemblyIdentity(
            string name,
            Version version,
            string culture,
            ImmutableArray<byte> publicKeyOrToken)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Version = version ?? ZeroVersion;
            Culture = String.IsNullOrEmpty(culture) ? null : culture;

            if (!publicKeyOrToken.IsEmpty)
            {
                PublicKeyToken = publicKeyOrToken.Length == 8
                    ? publicKeyOrToken
                    : GetPublicKeyToken(publicKeyOrToken);
            }
        }

        public override string ToString() => FullName;

        public static AssemblyIdentity Parse(string fullName)
        {
            if (fullName == null)
            {
                throw new ArgumentNullException(nameof(fullName));
            }

            var parts = fullName.Split(',');

            if (parts.Length == 0)
            {
                return null;
            }

            var name = parts[0].Trim();

            Version version = null;
            string culture = null;
            ImmutableArray<byte> publicKeyToken = ImmutableArray<byte>.Empty;

            if (parts.Length > 1)
            {
                for (int i = 1; i < parts.Length; i++)
                {
                    var keyValueParts = parts[i].Split('=');

                    if (keyValueParts.Length != 2)
                    {
                        throw new InvalidOperationException();
                    }

                    var key = keyValueParts[0].Trim();
                    var value = keyValueParts[1].Trim();

                    switch (key.ToUpperInvariant())
                    {
                        case "VERSION":
                            version = Version.Parse(value);
                            break;
                        case "CULTURE":
                            culture = value;
                            break;
                        case "PUBLICKEYTOKEN":

                            if (value.Length != 16)
                            {
                                throw new InvalidOperationException();
                            }

                            var builder = ImmutableArray.CreateBuilder<byte>();

                            for (int j = 0; j < 16; j += 2)
                            {
                                builder.Add(
                                    Byte.Parse(value.Substring(j, 2),
                                    NumberStyles.HexNumber,
                                    CultureInfo.InvariantCulture));
                            }

                            publicKeyToken = builder.ToImmutable();

                            break;

                        default:
                            throw new InvalidOperationException();
                    }
                }
            }

            return new AssemblyIdentity(name, version, culture, publicKeyToken);
        }

        public static AssemblyIdentity FromAssemblyName(AssemblyName assemblyName)
        {
            if (assemblyName == null)
            {
                throw new ArgumentNullException(nameof(assemblyName));
            }

            return new AssemblyIdentity(
                assemblyName.Name,
                assemblyName.Version,
                assemblyName.CultureName,
                assemblyName.GetPublicKeyToken()?.ToImmutableArray() ?? ImmutableArray<byte>.Empty);
        }

        internal static AssemblyIdentity FromAssemblyDefinition(
            MetadataReader metadataReader,
            AssemblyDefinition assemblyDefinition) =>
            new AssemblyIdentity(
                metadataReader.GetString(assemblyDefinition.Name),
                assemblyDefinition.Version,
                metadataReader.GetString(assemblyDefinition.Culture),
                metadataReader.GetBlobContent(assemblyDefinition.PublicKey));

        internal static AssemblyIdentity FromAssemblyReference(
            MetadataReader metadataReader,
            AssemblyReference assemblyReference) =>
            new AssemblyIdentity(
                metadataReader.GetString(assemblyReference.Name),
                assemblyReference.Version,
                metadataReader.GetString(assemblyReference.Culture),
                metadataReader.GetBlobContent(assemblyReference.PublicKeyOrToken));

        private string GetTokenAsString()
        {
            var builder = new StringBuilder(PublicKeyToken.Length * 2);

            foreach (var b in PublicKeyToken)
            {
                builder.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        private static ImmutableArray<byte> GetPublicKeyToken(
            ImmutableArray<byte> publicKey)
        {
#pragma warning disable CA5350 // Do not use insecure cryptographic algorithm SHA1.
            using (var sha1 = SHA1.Create())
#pragma warning restore CA5350 // Do not use insecure cryptographic algorithm SHA1.
            {
                var hash = sha1.ComputeHash(publicKey.ToArray());
                var builder = ImmutableArray.CreateBuilder<byte>();

                for (int i = 1; i <= 8; i++)
                {
                    builder.Add(hash[hash.Length - i]);
                }

                return builder.ToImmutable();
            }
        }
    }
}
