// The original code came from http://igoro.com/archive/skip-lists-are-fascinating/,
// However, it barely resembles it anymore.
// I retrieved and made the initial version of this March 17, 2011.
// Last modified: March 18, 2011.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Orvid.Collections
{
    public class SkipList<T>
    {
        protected Node _head = new Node(Array.Empty<byte>(), default(T), 33);
        /// <summary>
        /// The main node that the rest of the list is based on.
        /// </summary>
        public Node Head
        {
            get
            {
                return _head;
            }
        }
        protected Random _rand = new Random();
        public Random Random
        {
            get
            {
                return _rand;
            }
        }
        protected int _levels = 1;
        /// <summary>
        /// An Int32 representing how deep the list is.
        /// </summary>
        public int Levels
        {
            get
            {
                return _levels;
            }
        }

        public SkipList()
        {
            _head = new Node(Array.Empty<byte>(), default(T), 33);
            _rand = new Random();
            _levels = 1;
        }

        /// <summary>
        /// Adds an item to the skip list.
        /// </summary>
        public void Add(string key, T value)
        {
            Insert(key, value);
        }

        /// <summary>
        /// Inserts an item into the skip list.
        /// </summary>
        public void Insert(string key, T value)
        {
            byte[] key2 = Encoding.ASCII.GetBytes(key);
            int level = 0;
            for (int R = _rand.Next(); (R & 1) == 1; R >>= 1)
            {
                level++;
                if (level == _levels)
                {
                    _levels++;
                    break;
                }
            }
            var newNode = new Node(key2, value, level + 1);
            var cur = _head;
            for (int i = _levels - 1; i >= 0; i--)
            {
                for (; cur.Next[i] != null; cur = cur.Next[i])
                {
                    if (ArrayGreaterThan(cur.Next[i].Key, key2))
                    {
                        break;
                    }
                }

                if (i <= level)
                {
                    newNode.Next[i] = cur.Next[i];
                    cur.Next[i] = newNode;
                }
            }
        }

        public void Clear()
        {
            _head = new Node(Array.Empty<byte>(), default(T), 33);
            _rand = new Random();
            _levels = 1;
            GC.Collect();
        }

        /// <summary>
        /// Returns whether a particular key already exists in the skip list
        /// </summary>
        public bool Contains(string key, out T value)
        {
            byte[] value2 = Encoding.ASCII.GetBytes(key);
            for (int i = _levels - 1; i >= 0; i--)
            {
                for (var cur = _head; cur.Next[i] != null; cur = cur.Next[i])
                {
                    if (ArrayGreaterThan(cur.Next[i].Key, value2))
                    {
                        break;
                    }
                    else if (ArraysEqual(cur.Next[i].Key, value2))
                    {
                        value = cur.Next[i].Value;
                        return true;
                    }
                }
            }
            value = default(T);
            return false;
        }

        /// <summary>
        /// Attempts to remove one occurrence of a particular key from the skip list. Returns
        /// whether the key was found in the skip list.
        /// </summary>
        public bool Remove(string key)
        {
            byte[] value2 = Encoding.ASCII.GetBytes(key);

            bool found = false;
            for (int i = _levels - 1; i >= 0; i--)
            {
                for (var cur = _head; cur.Next[i] != null; cur = cur.Next[i])
                {
                    if (ArraysEqual(cur.Next[i].Key, value2))
                    {
                        found = true;
                        cur.Next[i] = cur.Next[i].Next[i];
                        break;
                    }

                    if (ArrayGreaterThan(cur.Next[i].Key, value2))
                    {
                        break;
                    }
                }
            }

            return found;
        }

        public IEnumerator<T> GetEnumerator()
        {
            var cur = _head.Next[0];
            while (cur != null)
            {
                yield return cur.Value;
                cur = cur.Next[0];
            }
        }

        private static bool ArraysEqual(byte[] firstArray, byte[] secondArray)
        {
            if (firstArray.Length != secondArray.Length)
            {
                return false;
            }
            var curlen = 0;
            foreach (byte b in firstArray)
            {
                if (b != secondArray[curlen])
                {
                    return false;
                }
                curlen++;
            }
            return true;
        }

        private static bool ArrayGreaterThan(byte[] firstArray, byte[] secondArray)
        {
            if (firstArray.Length != secondArray.Length)
            {
                return (firstArray.Length > secondArray.Length);
            }
            else
            {
                var n1 = 0;
                foreach (byte b in firstArray)
                {
                    if (b != secondArray[n1])
                    {
                        if (b > secondArray[n1])
                        {
                            return true;
                        }
                        else if (b < secondArray[n1])
                        {
                            return false;
                        }
                    }
                    else
                    {
                        n1++;
                    }
                }
                return true;
            }
        }

        [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Scope = "member")]
        [SuppressMessage("Design", "CA1034:Nested types should not be visible", Scope = "type")]
        public class Node
        {
            public Node[] Next { get; }
            public byte[] Key { get; }
            public T Value { get; }

            public Node(byte[] key, T value, int level)
            {
                Key = key;
                Value = value;
                Next = new Node[level];
            }
        }
    }
}
