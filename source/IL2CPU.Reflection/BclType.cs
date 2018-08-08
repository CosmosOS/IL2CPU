namespace IL2CPU.Reflection
{
    public class BclType
    {
        private const string SystemNamespace = "System";
        private const string SystemCollectionsGenericNamespace = "System.Collections.Generic";

        public static readonly BclType Void = new BclType(SystemNamespace, nameof(Void));
        public static readonly BclType Boolean = new BclType(SystemNamespace, nameof(Boolean));
        public static readonly BclType Char = new BclType(SystemNamespace, nameof(Char));
        public static readonly BclType SByte = new BclType(SystemNamespace, nameof(SByte));
        public static readonly BclType Byte = new BclType(SystemNamespace, nameof(Byte));
        public static readonly BclType Int16 = new BclType(SystemNamespace, nameof(Int16));
        public static readonly BclType UInt16 = new BclType(SystemNamespace, nameof(UInt16));
        public static readonly BclType Int32 = new BclType(SystemNamespace, nameof(Int32));
        public static readonly BclType UInt32 = new BclType(SystemNamespace, nameof(UInt32));
        public static readonly BclType Int64 = new BclType(SystemNamespace, nameof(Int64));
        public static readonly BclType UInt64 = new BclType(SystemNamespace, nameof(UInt64));
        public static readonly BclType Single = new BclType(SystemNamespace, nameof(Single));
        public static readonly BclType Double = new BclType(SystemNamespace, nameof(Double));
        public static readonly BclType String = new BclType(SystemNamespace, nameof(String));
        public static readonly BclType TypedReference = new BclType(SystemNamespace, nameof(TypedReference));
        public static readonly BclType IntPtr = new BclType(SystemNamespace, nameof(IntPtr));
        public static readonly BclType UIntPtr = new BclType(SystemNamespace, nameof(UIntPtr));
        public static readonly BclType Object = new BclType(SystemNamespace, nameof(Object));

        public static readonly BclType Array = new BclType(SystemNamespace, nameof(Array));
        public static readonly BclType Delegate = new BclType(SystemNamespace, nameof(Delegate));
        public static readonly BclType Enum = new BclType(SystemNamespace, nameof(Enum));
        public static readonly BclType ValueType = new BclType(SystemNamespace, nameof(ValueType));

        public static readonly BclType Type = new BclType(SystemNamespace, nameof(Type));

        public static readonly BclType IListOfT = new BclType(SystemCollectionsGenericNamespace, "IList`1");
        public static readonly BclType ICollectionOfT = new BclType(SystemCollectionsGenericNamespace, "ICollection`1");
        public static readonly BclType IEnumerableOfT = new BclType(SystemCollectionsGenericNamespace, "IEnumerable`1");
        public static readonly BclType IReadOnlyListOfT = new BclType(SystemCollectionsGenericNamespace, "IReadOnlyList`1");
        public static readonly BclType IReadOnlyCollectionOfT = new BclType(SystemCollectionsGenericNamespace, "IReadOnlyCollection`1");

        public string Namespace { get; }
        public string Name { get; }

        private BclType(string typeNamespace, string name)
        {
            Namespace = typeNamespace;
            Name = name;
        }
    }
}
