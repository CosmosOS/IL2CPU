using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;

using IL2CPU.Reflection.Debug;
using IL2CPU.Reflection.Internal;

namespace IL2CPU.Reflection
{
    public class MethodInfo : MemberInfo
    {
        public override ModuleInfo Module => ResolvedDefinition.Module;

        public override int MetadataToken => ResolvedDefinition.MetadataToken;

        public override TypeInfo DeclaringType { get; }

        public string Name => ResolvedDefinition.Name;

        public IReadOnlyList<GenericParameter> GenericParameters => _genericParameters.Value;
        public int GenericParameterCount =>
            _genericParameters.IsValueCreated ? GenericParameters.Count : ResolvedDefinition.GenericParameterCount;

        public IReadOnlyList<TypeInfo> GenericArguments { get; }

        public bool ContainsGenericParameters => GenericArguments.Any(a => a == null);

        public IReadOnlyList<TypeInfo> ParameterTypes => _signature.Value.ParameterTypes;
        public TypeInfo ReturnType => _signature.Value.ReturnType;
        
        public IReadOnlyList<ParameterInfo> Parameters => ResolvedDefinition.Parameters;
        public ParameterInfo ReturnParameter => ResolvedDefinition.ReturnParameter;

        public MethodBody MethodBody { get; }

        public MethodDebugInfo DebugInfo => ResolvedDefinition.DebugInfo;

        public override IReadOnlyCollection<CustomAttributeInfo> CustomAttributes => ResolvedDefinition.CustomAttributes;

        public MethodAttributes Attributes => ResolvedDefinition.Attributes;
        public MethodImplAttributes ImplAttributes => ResolvedDefinition.ImplAttributes;

        #region Method attribute wrappers

        public bool IsAbstract => HasFlag(MethodAttributes.Abstract);
        public bool IsAssembly => HasFlag(MethodAttributes.MemberAccessMask, MethodAttributes.Assembly);
        public bool IsCheckAccessOnOverride => HasFlag(MethodAttributes.CheckAccessOnOverride);
        public bool IsFamANDAssem => HasFlag(MethodAttributes.MemberAccessMask, MethodAttributes.FamANDAssem);
        public bool IsFamily => HasFlag(MethodAttributes.MemberAccessMask, MethodAttributes.Family);
        public bool IsFamORAssem => HasFlag(MethodAttributes.MemberAccessMask, MethodAttributes.FamORAssem);
        public bool IsFinal => HasFlag(MethodAttributes.Final);
        public bool IsHasSecurity => HasFlag(MethodAttributes.HasSecurity);
        public bool IsHideBySig => HasFlag(MethodAttributes.HideBySig);
        public bool IsNewSlot => HasFlag(MethodAttributes.NewSlot);
        public bool IsPinvokeImpl => HasFlag(MethodAttributes.PinvokeImpl);
        public bool IsPrivate => HasFlag(MethodAttributes.MemberAccessMask, MethodAttributes.Private);
        public bool IsPrivateScope => HasFlag(MethodAttributes.MemberAccessMask, MethodAttributes.PrivateScope);
        public bool IsPublic => HasFlag(MethodAttributes.MemberAccessMask, MethodAttributes.Public);
        public bool IsRequireSecObject => HasFlag(MethodAttributes.RequireSecObject);
        public bool IsReuseSlot => HasFlag(MethodAttributes.ReuseSlot);
        public bool IsRTSpecialName => HasFlag(MethodAttributes.RTSpecialName);
        public bool IsSpecialName => HasFlag(MethodAttributes.SpecialName);
        public bool IsStatic => HasFlag(MethodAttributes.Static);
        public bool IsUnmanagedExport => HasFlag(MethodAttributes.UnmanagedExport);
        public bool IsVirtual => HasFlag(MethodAttributes.Virtual);

        #endregion

        #region Method impl attribute wrappers

        public bool IsImplAggressiveInlining => HasImplFlag(MethodImplAttributes.AggressiveInlining);
        public bool IsImplForwardRef => HasImplFlag(MethodImplAttributes.ForwardRef);
        public bool IsImplIL => HasImplFlag(MethodImplAttributes.CodeTypeMask, MethodImplAttributes.IL);
        public bool IsImplInternalCall => HasImplFlag(MethodImplAttributes.InternalCall);
        public bool IsImplManaged => HasImplFlag(MethodImplAttributes.ManagedMask, MethodImplAttributes.Managed);
        public bool IsImplNative => HasImplFlag(MethodImplAttributes.CodeTypeMask, MethodImplAttributes.Native);
        public bool IsImplNoInlining => HasImplFlag(MethodImplAttributes.NoInlining);
        public bool IsImplNoOptimization => HasImplFlag(MethodImplAttributes.NoOptimization);
        public bool IsImplOPTIL => HasImplFlag(MethodImplAttributes.CodeTypeMask, MethodImplAttributes.OPTIL);
        public bool IsImplPreserveSig => HasImplFlag(MethodImplAttributes.PreserveSig);
        public bool IsImplRuntime => HasImplFlag(MethodImplAttributes.CodeTypeMask, MethodImplAttributes.Runtime);
        public bool IsImplSynchronized => HasImplFlag(MethodImplAttributes.Synchronized);
        public bool IsImplUnmanaged => HasImplFlag(MethodImplAttributes.ManagedMask, MethodImplAttributes.Unmanaged);

        #endregion

        public bool IsGenericMethod => GenericParameters.Count != 0;
        public bool IsGenericMethodDefinition => IsGenericMethod && GenericArguments.All(a => a == null);

        public bool IsConstructor => Name == ".ctor" && !IsStatic && IsRTSpecialName && IsSpecialName;
        public bool IsTypeInitializer => Name == ".cctor" && IsStatic && IsRTSpecialName && IsSpecialName;

        internal ResolvedMethodDefinition ResolvedDefinition { get; }
        internal GenericContext GenericContext { get; }

        private readonly Lazy<IReadOnlyList<GenericParameter>> _genericParameters;

        private readonly Lazy<MethodSignature<TypeInfo>> _signature;

        internal MethodInfo(
            ResolvedMethodDefinition resolvedDefinition,
            TypeInfo declaringType,
            IReadOnlyList<TypeInfo> typeArguments)
        {
            ResolvedDefinition = resolvedDefinition;

            DeclaringType = declaringType;

            _genericParameters = new Lazy<IReadOnlyList<GenericParameter>>(ResolveGenericParameters);

            _signature = new Lazy<MethodSignature<TypeInfo>>(DecodeMethodSignature);

            GenericContext = DeclaringType?.GenericContext ?? GenericContext.Empty;

            if (typeArguments != null
                && typeArguments.Count > 0)
            {
                GenericArguments = typeArguments;
                GenericContext = GenericContext.WithMethodArguments(typeArguments);
            }
            else if (GenericParameterCount > 0)
            {
                GenericArguments = new TypeInfo[GenericParameterCount];
            }
            else
            {
                GenericArguments = Array.Empty<TypeInfo>();
            }

            if (ResolvedDefinition.MethodBodyBlock != null)
            {
                MethodBody = new MethodBody(Module, ResolvedDefinition.MethodBodyBlock, GenericContext);
            }
        }

        public MethodInfo MakeGenericMethod(params TypeInfo[] typeArguments) =>
            MakeGenericMethod((IReadOnlyList<TypeInfo>)typeArguments);

        public MethodInfo MakeGenericMethod(IReadOnlyList<TypeInfo> typeArguments)
        {
            if (!IsGenericMethodDefinition)
            {
                throw new InvalidOperationException("MakeGenericMethod can only be called on generic method definitions!");
            }

#pragma warning disable CA1062 // Validate arguments of public methods
            if (GenericParameters.Count != typeArguments.Count)
#pragma warning restore CA1062 // Validate arguments of public methods
            {
                throw new InvalidOperationException("The type argument count should be the same as the generic parameter count!");
            }

            return new MethodInfo(ResolvedDefinition, DeclaringType, typeArguments);
        }

        public bool Matches(MethodInfo other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            return Matches(other.Name, other._signature.Value);
        }

        public MethodInfo GetGenericMethodDefinition()
        {
            if (IsGenericMethodDefinition)
            {
                return this;
            }

            return new MethodInfo(ResolvedDefinition, DeclaringType, null);
        }

        public override int GetHashCode() =>
            (DeclaringType?.GetHashCode() ?? 0) ^ Name.GetHashCode();

        public override bool Equals(object obj) =>
            obj is MethodInfo method
            && method.DeclaringType == DeclaringType
            && method.Matches(this)
            && method.GenericArguments.SequenceEqual(GenericArguments);

        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.Append(DeclaringType.ToString());
            builder.Append('.');
            builder.Append(Name);

            if (IsGenericMethod)
            {
                builder.Append('<');
                builder.Append(String.Join(", ", GenericArguments));
                builder.Append('>');
            }

            builder.Append('(');
            builder.Append(String.Join(", ", ParameterTypes));
            builder.Append(')');

            return builder.ToString();
        }

        internal bool Matches(string name, MethodSignature<TypeInfo> signature) =>
            Name == name
            && _signature.Value.Matches(signature);

        private IReadOnlyList<GenericParameter> ResolveGenericParameters() =>
            ResolvedDefinition.ResolveGenericParameters(GenericContext);

        private MethodSignature<TypeInfo> DecodeMethodSignature() => ResolvedDefinition.DecodeSignature(GenericContext);

        private bool HasFlag(MethodAttributes flag) => (Attributes & flag) != 0;
        private bool HasFlag(MethodAttributes mask, MethodAttributes flag) => (Attributes & mask) == flag;

        private bool HasImplFlag(MethodImplAttributes flag) => (ImplAttributes & flag) != 0;
        private bool HasImplFlag(MethodImplAttributes mask, MethodImplAttributes flag) => (ImplAttributes & mask) == flag;

        public static bool operator ==(MethodInfo method1, MethodInfo method2) =>
            EqualityComparer<MethodInfo>.Default.Equals(method1, method2);

        public static bool operator !=(MethodInfo method1, MethodInfo method2) => !(method1 == method2);
    }
}
