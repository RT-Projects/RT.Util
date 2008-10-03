using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RT.Util.ExtensionMethods
{
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Returns an enumeration of Tuple&lt;T, T&gt;s containing all pairs of elements from the source IEnumerable.
        /// For example, the input sequence 1, 2 yields the pairs [1,1], [1,2], [2,1], and [2,2].
        /// </summary>
        public static IEnumerable<Tuple<T, T>> AllPairs<T>(this IEnumerable<T> Source)
        {
            foreach (T item1 in Source)
                foreach (T item2 in Source)
                    yield return new Tuple<T, T>(item1, item2);
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
    }
}
