using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Cosmos.IL2CPU
{
    // Contains known types and methods, both scanned and unscanned
    // We need both a HashSet and a List. HashSet for speed of checking
    // to see if we already have it. And mItems contains an indexed list
    // so we can scan it as it changes. Foreach can work on HashSet,
    // but if foreach is used while its changed, a collection changed
    // exception will occur and copy on demand for each loop has too
    // much overhead.
    // we use a custom comparer, because the default Hashcode does not work.
    // In .NET 4.0 has the DeclaringType often changed to System.Object,
    // didn't sure if hashcode changed. The situation now in .NET 4.0
    // is that the Contains method in OurHashSet checked only the
    // default Hashcode. With adding DeclaringType in the Hashcode it runs.
    [SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix",
        Justification = "The type name has a correct suffix.", Scope = "type")]
    public class OurHashSet<T> : IEnumerable<T> where T : MemberInfo
    {
        private Dictionary<int, T> mItems = new Dictionary<int, T>();

        public bool Contains(T aItem)
        {
            if (aItem == null)
            {
                throw new ArgumentNullException(nameof(aItem));
            }

            return mItems.ContainsKey(GetHash(aItem));
        }

        public void Add(T aItem)
        {
            if (aItem == null)
            {
                throw new ArgumentNullException(nameof(aItem));
            }

            mItems.Add(GetHash(aItem), aItem);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return (from item in mItems
                    select item.Value).GetEnumerator();
        }

        public T GetItemInList(T aItem)
        {
            if (aItem == null)
            {
                throw new ArgumentNullException(nameof(aItem));
            }

            if (mItems.TryGetValue(GetHash(aItem), out var xResult))
            {
                return xResult;
            }
            else
            {
                return aItem;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (from item in mItems
                    select item.Value).GetEnumerator();
        }

        private static string GetDeclareTypeString(T item)
        {
            var xName = item.DeclaringType;
            return xName == null ? String.Empty : xName.ToString();
        }

        private static int GetHash(T item)
        {
            return (item.ToString() + GetDeclareTypeString(item)).GetHashCode();
        }
    }
}
