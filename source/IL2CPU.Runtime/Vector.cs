using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace IL2CPU.Runtime
{
#pragma warning disable CA1710 // Identifiers should have correct suffix
    public class Vector<T> : IList<T>, IReadOnlyList<T>
#pragma warning restore CA1710 // Identifiers should have correct suffix
    {
        public T this[int index]
        {
            get => Unsafe.As<T[]>(this)[index];
            set => Unsafe.As<T[]>(this)[index] = value;
        }

        public int Count => Unsafe.As<T[]>(this).Length;

        public bool IsReadOnly => true;

        public void Add(T item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();

        public bool Contains(T item) => IndexOf(item) != -1;

        public void CopyTo(T[] array, int arrayIndex)
        {
            var @this = Unsafe.As<T[]>(this);
            Array.Copy(@this, 0, array, arrayIndex, @this.Length);
        }

        public IEnumerator<T> GetEnumerator() => new VectorEnumerator(Unsafe.As<T[]>(this));

        public int IndexOf(T item) => Array.IndexOf(Unsafe.As<T[]>(this), item);

        public void Insert(int index, T item) => throw new NotSupportedException();
        public bool Remove(T item) => throw new NotSupportedException();
        public void RemoveAt(int index) => throw new NotSupportedException();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private class VectorEnumerator : IEnumerator<T>
        {
            public T Current => _array[_index];
            object IEnumerator.Current => Current;

            private readonly T[] _array;
            private int _index;

            public VectorEnumerator(T[] array)
            {
                _array = array;
                _index = -1;
            }

            public bool MoveNext()
            {
                var index = _index + 1;
                var result = index < _array.Length;

                if (result)
                {
                    _index = index;
                }

                return result;
            }

            public void Reset() => _index = -1;

            public void Dispose() { }
        }
    }
}
