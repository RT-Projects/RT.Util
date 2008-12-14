using System;
using System.Collections.Generic;
using System.Linq;
using RT.Util.Collections;

namespace RT.Util.ExtensionMethods
{
    /// <summary>
    /// Provides extension methods on the <see cref="IEnumerable&lt;T&gt;"/> type.
    /// </summary>
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Returns an enumeration of Tuple&lt;T, T&gt;s containing all pairs of elements from the source IEnumerable.
        /// For example, the input sequence 1, 2 yields the pairs [1,1], [1,2], [2,1], and [2,2].
        /// </summary>
        public static IEnumerable<Tuple<T, T>> AllPairs<T>(this IEnumerable<T> Source)
        {
            return Source.Join(Source);
        }

        /// <summary>
        /// Returns an enumeration of Tuple&lt;T, U&gt;s containing all ordered pairs of elements from the two source IEnumerables.
        /// For example, [1, 2].Join(["one", "two"]) results in the tuples [1, "one"], [1, "two"], [2, "one"] and [2, "two"].
        /// </summary>
        public static IEnumerable<Tuple<T, U>> Join<T, U>(this IEnumerable<T> Source, IEnumerable<U> With)
        {
            foreach (T item1 in Source)
                foreach (U item2 in With)
                    yield return new Tuple<T, U>(item1, item2);
        }

        /// <summary>
        /// Returns an enumeration of Tuple&lt;T, T&gt;s containing all unique pairs of distinct elements from the source IEnumerable.
        /// For example, the input sequence 1, 2, 3 yields the pairs [1,2], [1,3] and [2,3] only.
        /// </summary>
        public static IEnumerable<Tuple<T, T>> UniquePairs<T>(this IEnumerable<T> Source)
        {
            int i = 0;
            foreach (T item1 in Source)
            {
                i++;
                int j = 0;
                foreach (T item2 in Source)
                {
                    j++;
                    if (j <= i)
                        continue;
                    yield return new Tuple<T, T>(item1, item2);
                }
            }
        }

        /// <summary>
        /// Returns an enumeration of the specified enumerable in sorted order. Note that
        /// the entire source will be consumed before a single element is returned.
        /// </summary>
        public static IEnumerable<T> Sorted<T>(this IEnumerable<T> source) where T: IComparable<T>
        {
            T[] arr = source.ToArray();
            Array.Sort<T>(arr);
            foreach (T item in arr)
                yield return item;
        }

        /// <summary>
        /// Returns an enumeration of the specified enumerable in sorted order. Note that
        /// the entire source will be consumed before a single element is returned.
        /// </summary>
        public static IEnumerable<T> Sorted<T>(this IEnumerable<T> source, Comparison<T> comparison)
        {
            T[] arr = source.ToArray();
            Array.Sort<T>(arr, comparison);
            foreach (T item in arr)
                yield return item;
        }

        /// <summary>
        /// Compares this IEnumerable to another one. The two IEnumerables are only deemed equal if the
        /// number of items contained in them is the same, and all items are equal and come in the same order.
        /// </summary>
        public static bool EqualItems<T>(this IEnumerable<T> one, IEnumerable<T> other) where T: IEquatable<T>
        {
            var enum1 = one.GetEnumerator();
            var enum2 = other.GetEnumerator();
            bool havemore1, havemore2;
            while (true)
            {
                havemore1 = enum1.MoveNext();
                havemore2 = enum2.MoveNext();
                if (!havemore1 || !havemore2)
                    return havemore1 == havemore2;
                if (!enum1.Current.Equals(enum2.Current))
                    return false;
            }
        }
    }
}
