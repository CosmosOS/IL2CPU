using System;
using System.Collections.Generic;

namespace Cosmos.IL2CPU
{
    public class SZArrayImpl<T>
    {
        public static IEnumerator<T> GetEnumerator(T[] aThis)
        {
            foreach (var item in aThis)
            {
                yield return item;
            }
        }

        public static void CopyTo(T[] aThis, T[] aArray, int aIndex)
        {
            aThis.CopyTo(aArray, aIndex);
        }

        public static int get_Count(T[] aThis)
        {
            return aThis.Length;
        }

        public static T get_Item(T[] aThis, int aIndex)
        {
            return aThis[aIndex];
        }

        public static void set_Item(T[] aThis, int aIndex, T aValue)
        {
            aThis[aIndex] = aValue;
        }

        public static void Add(T[] aThis, T aValue)
        {
            throw new NotSupportedException();
        }

        public static bool Contains(T[] aThis, T aValue)
        {
            for (var i = 0; i < aThis.Length; i++)
            {
                if(aThis[i].Equals(aValue))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool get_IsReadOnly(T[] aThis)
        {
            return true;
        }

        public static void Clear(T[] aThis)
        {
            throw new NotSupportedException();
        }

        public static int IndexOf(T[] aThis, T aValue)
        {
        //Broken until net 5.0 call virt improvement ist merged
            for (int i = 0; i < aThis.Length; i++)
            {
                if (aThis[i].Equals(aValue))
                {
                    return i;
                }
            }
            return -1;
        }

        public static void Insert(T[] aThis, int aIndex, T aValue)
        {
            throw new NotSupportedException();
        }

        public static void RemoveAt(T[] aThis, int aIndex)
        {
            throw new NotSupportedException();
        }

        public static bool get_IsFixedSize(T[] aThis)
        {
            return true;
        }

        public static void Remove(T[] aThis, T aValue)
        {
            throw new NotSupportedException();
        }

        public static object get_SyncRoot(T[] aThis)
        {
            throw new NotSupportedException();
        }

        public static bool get_IsSynchronized(T[] aThis)
        {
            return true;
        }
    }
}