using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace IL2CPU.Reflection.Tests
{
    internal static class SampleObjects
    {
        [NameTag(Name = nameof(SampleObject))]
        internal class SampleObject : SampleParent
        {
        }

        [NameTag(Name = nameof(SampleParent))]
        [FatherTag(Last = nameof(SampleParent))]
        [SingleTextTag(Text = nameof(SampleParent))]
        [MultiTextTag(Text = nameof(SampleParent))]
        internal class SampleParent : SampleBase
        {
        }

        [SingleTextTag(Text = nameof(SampleBase))]
        [MultiTextTag(Text = nameof(SampleBase))]
        [MultiTextTag(Text = nameof(SampleBase))]
        internal abstract class SampleBase
        {
        }

        [AttributeUsage(AttributeTargets.Class, Inherited = false)]
        public sealed class NameTag : Attribute
        {
            public string Name;
        }

        [AttributeUsage(AttributeTargets.Class, Inherited = false)]
        public sealed class FatherTag : Attribute
        {
            public string Last;
        }

        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
        public sealed class MultiTextTag : Attribute
        {
            public string Text;
        }

        [AttributeUsage(AttributeTargets.Class)]
        public sealed class SingleTextTag : Attribute
        {
            public string Text;
        }

        [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
        public sealed class ParamOffset : Attribute
        {
            public ParamOffset(int offset) => Value = offset;

            public int Value { get; }
        }

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
        public sealed class MethodOffset : Attribute
        {
            public MethodOffset(int offset) => Value = offset;

            public int Value { get; }
        }

        [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
        public sealed class StoreOffset : Attribute
        {
            public StoreOffset(int offset) => Value = offset;

            public int Value { get; }
        }

        internal abstract class ArrayImpl
        {
            [StoreOffset(5)] public uint IntVal;

            [MethodOffset(1)]
            public abstract int GetUpperBound([ParamOffset(3)] Array array);
        }

        internal class BetterArrayImpl : ArrayImpl
        {
            [StoreOffset(6)] public uint IntVal;

            [MethodOffset(2)]
            public override int GetUpperBound([ParamOffset(4)] Array array)
            {
                throw new InvalidOperationException(array + " ?");
            }
        }

        public class WeirdTyping
        {
            private class SuperDuper<T>
            {
                public T PublicField;

                internal int OtherField;

                public R GetSomething<R>(T arg) => default;

                public T GetNothing() => default;

                public void ExecuteMe<S>() where S : Enum
                {
                    OtherField = OtherField + 1;
                    PublicField = (T)(object)23;
                }

                public void ExecuteMe()
                {
                    OtherField = OtherField + 2;
                    PublicField = default;
                }
            }

            public bool Marker;

            public static IEnumerator<T> GetEnumerator<T>()
            {
                T[] array = new T[5];
                int num = array.Length;
                if (num != 0)
                {
                    return new List<T>.Enumerator();
                }

                var self = GetEnumerator<ulong>();
                self.MoveNext();
                var val = self.Current;
                Console.WriteLine(val);
                return (IEnumerator<T>)(object)self;
            }

            public static string DoEdgeCases()
            {
                var array = new[] { 1, 2, 3, 4, 5 };
                var index = Array.IndexOf(array, 3);
                var obj = new SuperDuper<UInt32>();
                var pub = obj.PublicField + 2;
                obj.OtherField = (int)pub;
                var sth = obj.GetSomething<string>(3);
                var good = obj.GetNothing() + "?";
                obj.ExecuteMe<OpCodeType>();
                var tmp1 = String.Empty;
                var tmp2 = Math.PI;
                obj.ExecuteMe();
                IStrongBox box1 = new StrongBox<uint>();
                var tmp3 = box1.Value + " " + ((StrongBox<uint>)box1).Value;
                var vt1 = ValueTuple.Create('1', 2, true);
                var tmp4 = vt1.Item1 + " " + vt1.Item2 + " " + vt1.Item3;
                var vt2 = ValueTuple.Create(UInt32.MaxValue);
                return index + " ! " + sth + " ? " + good + " / " + tmp1 + " " + tmp2 + " " + tmp3 + " " + tmp4 + " " + vt2.Item1;
            }

            public static string RenderComplex()
            {
                const int x = 119;
                var list = new List<Action<bool, int, double>>(x)
                {
                    (b, i, a) =>
                    {
                        Console.WriteLine(b);
                        Console.WriteLine(i);
                        Console.WriteLine(a);
                        Console.WriteLine("End!");
                    }
                };
                var set = new HashSet<object>(list);
                var dict = new SortedDictionary<string, ICollection<object>>
                {
                    {"hello", set}
                };
                var wrap = (Tuple<ulong, Type>)(object)dict;
                var w1 = (List<Action<bool, int, double>>)(object)wrap;
                var w2 = (HashSet<object>)(object)w1;
                var w3 = (SortedDictionary<string, ICollection<object>>)(object)w2;
                var w4 = (Stream)(object)w3;
                var w5 = (ICollection)w4;
                var w6 = (ICollection<Nullable<float>[]>)w5;
                var json = JsonConvert.SerializeObject(dict, Formatting.Indented);

                Enum.TryParse("H", out OpCodeType opCode);
                Console.WriteLine(opCode);
                Console.WriteLine((int)opCode);
                Console.WriteLine((long)opCode);

                var arry1 = new[] { 1, 3, 4, 5, 5, 67, 8 };
                var max1 = arry1.Max();
                var arry2 = new object[] { arry1, arry1, max1 };
                var max3 = arry2.ToArray();
                var arry3 = (int[])(object)arry2;
                var max5 = (KeyValuePair<Type, Type>[])(object)arry3;

                return json + " " + wrap + " " + max3 + " " + max5;
            }

            public static string RenderPrimitives()
            {
                const int x = 119;
                var u8 = (Nullable<byte>)x;
                var s8 = (Nullable<sbyte>)x;
                var s16 = (Nullable<Int16>)x;
                var s32 = (Nullable<Int32>)x;
                var s64 = (Nullable<Int64>)x;
                var u16 = (Nullable<UInt16>)x;
                var u32 = (Nullable<UInt32>)x;
                var u64 = (Nullable<UInt64>)x;
                var bo = (Nullable<Boolean>)(x >= 50);
                var ch = (Nullable<Char>)x;
                var f32 = (Nullable<Single>)x;
                var f64 = (Nullable<Double>)x;
                var st = new[] { x + "" };
                var ty = st.GetType();
                var ip = (Nullable<IntPtr>)x;
                var uip = (Nullable<UIntPtr>)x;
                var obj = (Object)x;
                var yz = JsonConvert.DeserializeObject<Tuple<string, Type, Object>>("?");
                return $"{u8} {s8} {s16} {s32} {s64} {u16} {u32} {u64} " +
                       $"{bo} {ch} {f32} {f64} {ty} {ip} {uip} {obj} " +
                       $"{yz} ";
            }
        }

        interface Interf
        {
            string InterfaceImpl(int n);
        }

        public class BaseClass
        {
            public override string ToString()
            {
                return "Base";
            }

            public virtual void Method1()
            {
                Console.WriteLine("Method1");
            }

            public virtual void Method2()
            {
                Console.WriteLine("Method2");
            }

            public virtual void Method3()
            {
                Console.WriteLine("Method3");
            }
        }

        public class DerivedClass : BaseClass, Interf
        {
            public string InterfaceImpl(int n)
            {
                return n.ToString("N");
            }

            public override void Method2()
            {
                Console.WriteLine("Derived.Method2");
            }

            public new void Method3()
            {
                Console.WriteLine("Derived.Method3");
            }
        }

        internal sealed class ArrayEnumerator<T> : IEnumerator<T>
        {
            public bool MoveNext() => throw new NotImplementedException("MoveNext");

            public T Current => throw new NotImplementedException("Current");

            object IEnumerator.Current => Current;

            void IEnumerator.Reset() => throw new NotImplementedException("Reset");

            public void Dispose() => throw new NotImplementedException("Dispose");
        }

        internal class ComplexEnumerator : IEnumerable<string>, IEnumerable<bool>,
            IEnumerable<Tuple<bool, HashSet<List<string>>, long>>
        {
            public IEnumerator<string> GetEnumerator()
                => throw new NotImplementedException();

            IEnumerator IEnumerable.GetEnumerator()
                => throw new NotImplementedException();

            IEnumerator<bool> IEnumerable<bool>.GetEnumerator()
                => throw new NotImplementedException();

            IEnumerator<Tuple<bool, HashSet<List<string>>, long>>
                IEnumerable<Tuple<bool, HashSet<List<string>>, long>>.GetEnumerator()
                  => throw new NotImplementedException();
        }
    }
}
