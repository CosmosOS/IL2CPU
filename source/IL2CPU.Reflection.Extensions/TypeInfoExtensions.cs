using System;
using System.Collections.Generic;
using System.Linq;

using IL2CPU.Reflection.Types;

namespace IL2CPU.Reflection
{
    public static class TypeInfoExtensions
    {
        public static TypeInfo GetElementType(this TypeInfo type)
        {
            switch (type)
            {
                case ArrayType arrayType:
                    return arrayType.ElementType;
                case ByReferenceType byReferenceType:
                    return byReferenceType.ElementType;
                case PinnedType pinnedType:
                    return pinnedType.ElementType;
                case PointerType pointerType:
                    return pointerType.ElementType;
                case SZArrayType szArrayType:
                    return szArrayType.ElementType;
                default:
                    return null;
            }
        }

        public static TypeInfo GetEnumUnderlyingType(this TypeInfo type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return type.Fields.Single(f => !f.IsStatic).FieldType;
        }

        public static MethodInfo GetConstructor(
            this TypeInfo type,
            IReadOnlyList<TypeInfo> parameterTypes)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            foreach (var method in type.Methods)
            {
                if (method.IsConstructor
                    && method.ParameterTypes.SequenceEqual(parameterTypes))
                {
                    return method;
                }
            }

            return null;
        }

        public static MethodInfo GetTypeInitializer(this TypeInfo type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return type.Methods.SingleOrDefault(m => m.IsTypeInitializer);
        }

        public static MethodInfo GetMethod(
            this TypeInfo type,
            string name,
            IReadOnlyList<TypeInfo> parameterTypes,
            Func<MethodInfo, bool> match = null)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            foreach (var method in type.Methods)
            {
                if (method.Name == name)
                {
                    if (method.ParameterTypes.Count != parameterTypes.Count)
                    {
                        continue;
                    }

                    bool matches = true;

                    for (int i = 0; i < parameterTypes.Count; i++)
                    {
                        if (!TypeIsGenericParameter(parameterTypes[i])
                            && !TypeIsGenericParameter(method.ParameterTypes[i])
                            && parameterTypes[i] != method.ParameterTypes[i])
                        {
                            matches = false;
                            break;
                        }
                    }

                    if (!matches)
                    {
                        continue;
                    }
                    
                    if (match?.Invoke(method) ?? true)
                    {
                        return method;
                    }

                    break;
                }
            }

            bool TypeIsGenericParameter(TypeInfo typeInfo)
            {
                if (typeInfo == null)
                {
                    return true;
                }

                if (typeInfo is ArrayType
                    || typeInfo is ByReferenceType
                    || typeInfo is PinnedType
                    || typeInfo is PointerType
                    || typeInfo is SZArrayType)
                {
                    return TypeIsGenericParameter(typeInfo.GetElementType());
                }

                return false;
            }

            return null;
        }

        public static MethodInfo GetMethod(
            this TypeInfo type,
            string name,
            Func<MethodInfo, bool> match = null)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            foreach (var method in type.Methods)
            {
                if (method.Name == name)
                {
                    if (match?.Invoke(method) ?? true)
                    {
                        return method;
                    }

                    break;
                }
            }

            return null;
        }

        public static IEnumerable<MethodInfo> GetMethods(
            this TypeInfo type,
            Func<MethodInfo, bool> match = null)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            foreach (var method in type.Methods)
            {
                if (match?.Invoke(method) ?? true)
                {
                    yield return method;
                }
            }
        }

        public static FieldInfo GetField(
            this TypeInfo type,
            string name)
        {
            foreach (var field in type.GetFields())
            {
                if (field.Name == name)
                {
                    return field;
                }
            }

            return null;
        }

        public static IEnumerable<FieldInfo> GetFields(
            this TypeInfo type,
            Func<FieldInfo, bool> match = null)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (type.BaseType != null)
            {
                foreach (var field in type.BaseType.GetFields(match))
                {
                    yield return field;
                }
            }

            foreach (var field in type.Fields)
            {
                if (match?.Invoke(field) ?? true)
                {
                    yield return field;
                }
            }
        }

        public static IEnumerable<TypeInfo> GetImplementedInterfaces(this TypeInfo type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var explicitImplementedInterfaces = type.ExplicitImplementedInterfaces;

            var implementedInterfaces = new List<TypeInfo>(explicitImplementedInterfaces.Count);

            foreach (var explicitInterface in explicitImplementedInterfaces)
            {
                implementedInterfaces.Add(explicitInterface);
            }

            foreach (var explicitInterface in explicitImplementedInterfaces)
            {
                foreach (var interfaceImplementedInterface in explicitInterface.GetImplementedInterfaces())
                {
                    if (!implementedInterfaces.Contains(interfaceImplementedInterface))
                    {
                        implementedInterfaces.Add(interfaceImplementedInterface);
                    }
                }
            }

            if (type.BaseType != null)
            {
                foreach (var baseImplementedInterface in type.BaseType.GetImplementedInterfaces())
                {
                    if (!implementedInterfaces.Contains(baseImplementedInterface))
                    {
                        implementedInterfaces.Add(baseImplementedInterface);
                    }
                }
            }

            return implementedInterfaces;
        }

        public static bool IsAssignableFrom(
            this TypeInfo type,
            TypeInfo other)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            // todo: generic parameter for which the constraint is satisfied?
            return type == other
                || other.IsSubclassOf(type)
                || (type.IsInterface
                    && other.ImplementsInterface(type))
                || (other.BaseType != null
                    && type.IsAssignableFrom(other.BaseType))
                || (type.Namespace == "System" && type.Name == "Object"
                    && other.IsInterface);
        }

        public static bool ImplementsInterface(
            this TypeInfo type,
            TypeInfo interfaceType)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (interfaceType == null)
            {
                throw new ArgumentNullException(nameof(interfaceType));
            }

            if (!interfaceType.IsInterface)
            {
                throw new ArgumentException("The specified type is not an interface type!", nameof(interfaceType));
            }

            var explicitInterfaces = type.ExplicitImplementedInterfaces;

            if (explicitInterfaces.Contains(interfaceType))
            {
                return true;
            }

            foreach (var explicitInterface in explicitInterfaces)
            {
                if (explicitInterface.ImplementsInterface(interfaceType))
                {
                    return true;
                }
            }

            if (type.BaseType != null)
            {
                return type.BaseType.ImplementsInterface(interfaceType);
            }

            return false;
        }

        public static bool IsSubclassOf(
            this TypeInfo type,
            TypeInfo other)
        {
            var baseType = type;

            while (baseType != null)
            {
                if (baseType == other)
                {
                    return true;
                }

                baseType = baseType.BaseType;
            }

            return false;
        }
    }
}
