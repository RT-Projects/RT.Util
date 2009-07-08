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
        public static IEnumerable<Tuple<T, T>> AllPairs<T>(this IEnumerable<T> source)
        {
            return source.Join(source);
        }

        /// <summary>
        /// Returns an enumeration of Tuple&lt;T, U&gt;s containing all ordered pairs of elements from the two source IEnumerables.
        /// For example, [1, 2].Join(["one", "two"]) results in the tuples [1, "one"], [1, "two"], [2, "one"] and [2, "two"].
        /// </summary>
        public static IEnumerable<Tuple<T, U>> Join<T, U>(this IEnumerable<T> source, IEnumerable<U> with)
        {
            foreach (T item1 in source)
                foreach (U item2 in with)
                    yield return new Tuple<T, U>(item1, item2);
        }

        /// <summary>
        /// Returns an enumeration of Tuple&lt;T, T&gt;s containing all unique pairs of distinct elements from the source IEnumerable.
        /// For example, the input sequence 1, 2, 3 yields the pairs [1,2], [1,3] and [2,3] only.
        /// </summary>
        public static IEnumerable<Tuple<T, T>> UniquePairs<T>(this IEnumerable<T> source)
        {
            int i = 0;
            foreach (T item1 in source)
            {
                i++;
                int j = 0;
                foreach (T item2 in source)
                {
                    j++;
                    if (j <= i)
                        continue;
                    yield return new Tuple<T, T>(item1, item2);
                }
            }
        }

        /// <summary>
        /// Returns an enumeration of the specified enumerable in sorted order.
        /// </summary>
        public static IEnumerable<T> Order<T>(this IEnumerable<T> source)
        {
            return source.OrderBy(x => x);
        }

        /// <summary>
        /// Returns an enumeration of the specified enumerable in sorted order.
        /// </summary>
        public static IEnumerable<T> Order<T>(this IEnumerable<T> source, IComparer<T> comparison)
        {
            return source.OrderBy(x => x, comparison);
        }

        /// <summary>
        /// Returns a collection containing numberOfTimes references/copies of the specified object/struct.
        /// </summary>
        /// <typeparam name="T">Type of object to repeat.</typeparam>
        /// <param name="repeatWhat">Object or struct to repeat.</param>
        /// <param name="numberOfTimes">Number of times to repeat the object or struct.</param>
        public static IEnumerable<T> Repeat<T>(this T repeatWhat, int numberOfTimes)
        {
            while (numberOfTimes > 0)
            {
                yield return repeatWhat;
                numberOfTimes--;
            }
        }

        /// <summary>
        /// Splits the specified IEnumerable at every element that satisfies a specified predicate and returns
        /// a collection containing each sequence of elements in between each pair of such elements.
        /// The elements satisfying the predicate are not included.
        /// </summary>
        /// <param name="splitWhat">The collection to be split.</param>
        /// <param name="splitWhere">A predicate that determines which elements constitute the separators.</param>
        /// <returns>A collection containing the individual pieces taken from the original collection.</returns>
        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> splitWhat, Func<T, bool> splitWhere)
        {
            int prevIndex = 0;
            foreach (var index in splitWhat.Select((elem, ind) => new { e = elem, i = ind }).Where(x => splitWhere(x.e)))
            {
                yield return splitWhat.Skip(prevIndex).Take(index.i - prevIndex);
                prevIndex = index.i + 1;
            }
            yield return splitWhat.Skip(prevIndex);
        }

        /// <summary>
        /// Adds a single element to the end of an IEnumerable.
        /// </summary>
        /// <typeparam name="T">Type of enumerable to return.</typeparam>
        /// <returns>IEnumerable containing all the input elements, followed by the specified additional element.</returns>
        public static IEnumerable<T> Add<T>(this IEnumerable<T> input, T element)
        {
            foreach (var e in input)
                yield return e;
            yield return element;
        }

        /// <summary>
        /// Adds a single element to the start of an IEnumerable.
        /// </summary>
        /// <typeparam name="T">Type of enumerable to return.</typeparam>
        /// <returns>IEnumerable containing the specified additional element, followed by all the input elements.</returns>
        public static IEnumerable<T> Prepend<T>(this IEnumerable<T> input, T element)
        {
            yield return element;
            foreach (var e in input)
                yield return e;
        }

        /// <summary>
        /// This does the same as .Order(), but it is much faster if you intend to extract only the first few items using .Take().
        /// </summary>
        /// <param name="source">The sequence to be sorted.</param>
        /// <returns>The given IEnumerable&lt;T&gt; with its elements sorted progressively.</returns>
        public static IEnumerable<T> OrderLazy<T>(this IEnumerable<T> source)
        {
            return OrderLazy(source, Comparer<T>.Default);
        }

        /// <summary>
        /// This does the same as .Order(), but it uses HeapSort instead of QuickSort.
        /// This is faster if you intend to extract only the first few items using .Take().
        /// </summary>
        /// <param name="source">The sequence to be sorted.</param>
        /// <param name="comparer">An instance of <see cref="IComparer&lt;T&gt;"/> specifying the comparison to use on the items.</param>
        /// <returns>The given IEnumerable&lt;T&gt; with its elements sorted progressively.</returns>
        public static IEnumerable<T> OrderLazy<T>(this IEnumerable<T> source, IComparer<T> comparer)
        {
            var arr = source.ToArray();
            if (arr.Length < 2)
                return arr;
            int[] map = new int[arr.Length];
            for (int i = 0; i < arr.Length; i++)
                map[i] = i;
            return quickSort(arr, map, 0, arr.Length - 1, comparer);
        }

        private static int compareForStableSort<T>(T elem1, int elem1Index, T elem2, int elem2Index, IComparer<T> comparer)
        {
            int r = comparer.Compare(elem1, elem2);
            return r != 0 ? r : elem1Index.CompareTo(elem2Index);
        }

        private static IEnumerable<T> quickSort<T>(T[] items, int[] map, int left, int right, IComparer<T> comparer)
        {
            while (left < right)
            {
                int curleft = left;
                int curright = right;
                int pivotIndex = map[curleft + ((curright - curleft) >> 1)];
                T pivot = items[pivotIndex];
                do
                {
                    while ((curleft < map.Length) && compareForStableSort(pivot, pivotIndex, items[map[curleft]], map[curleft], comparer) > 0)
                        curleft++;
                    while ((curright >= 0) && compareForStableSort(pivot, pivotIndex, items[map[curright]], map[curright], comparer) < 0)
                        curright--;
                    if (curleft > curright)
                        break;

                    if (curleft < curright)
                    {
                        int tmp = map[curleft];
                        map[curleft] = map[curright];
                        map[curright] = tmp;
                    }
                    curleft++;
                    curright--;
                }
                while (curleft <= curright);
                if (left < curright)
                    foreach (var s in quickSort(items, map, left, curright, comparer))
                        yield return s;
                else if (left == curright)
                    yield return items[map[curright]];
                if (curright + 1 < curleft)
                    yield return items[map[curright + 1]];
                left = curleft;
            }
            yield return items[map[left]];
        }

        /// <summary>
        /// Returns all permutations of the input IEnumerable&lt;T&gt;.
        /// </summary>
        /// <param name="source">The list of items to permute.</param>
        /// <returns>IEnumerable&lt;IEnumerable&lt;T&gt;&gt; containing all permutations of the input IEnumerable&lt;T&gt;.</returns>
        public static IEnumerable<IEnumerable<T>> Permutations<T>(this IEnumerable<T> source)
        {
            // Ensure that the source IEnumerable is evaluated only once
            return permutations(source.ToArray());
        }

        private static IEnumerable<IEnumerable<T>> permutations<T>(IEnumerable<T> source)
        {
            var c = source.Count();
            if (c == 1)
                yield return source;
            else
                for (int i = 0; i < c; i++)
                    foreach (var p in permutations(source.Take(i).Concat(source.Skip(i + 1))))
                        yield return source.Skip(i).Take(1).Concat(p);
        }

        /// <summary>
        /// Returns all subsequences of the input IEnumerable&lt;T&gt;.
        /// </summary>
        /// <param name="source">The sequence of items to generate subsequences of.</param>
        /// <returns>IEnumerable&lt;IEnumerable&lt;T&gt;&gt; containing all subsequences of the input IEnumerable&lt;T&gt;.</returns>
        public static IEnumerable<IEnumerable<T>> Subsequences<T>(this IEnumerable<T> source)
        {
            // Ensure that the source IEnumerable is evaluated only once
            return subsequences(source.ToArray());
        }

        private static IEnumerable<IEnumerable<T>> subsequences<T>(IEnumerable<T> source)
        {
            if (source.Any())
            {
                foreach (var comb in subsequences(source.Skip(1)))
                {
                    yield return comb;
                    yield return source.Take(1).Concat(comb);
                }
            }
            else
            {
                yield return Enumerable.Empty<T>();
            }
        }

        /// <summary>
        /// Returns the first element of a sequence, or a default value if the sequence contains no elements.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The <see cref="IEnumerable&lt;T&gt;"/> to return the first element of.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="default">The default value to return if the sequence contains no elements.</param>
        /// <returns><paramref name="default"/> if <paramref name="source"/> is empty or if no element passes the test specified by <paramref name="predicate"/>;
        /// otherwise, the first element in <paramref name="source"/> that passes the test specified by <paramref name="predicate"/>.</returns>
        public static T FirstOrDefault<T>(this IEnumerable<T> source, Func<T, bool> predicate, T @default)
        {
            using (var e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                    return @default;
                return e.Current;
            }
        }

        /// <summary>
        /// Returns the first element of a sequence, or a default value if the sequence contains no elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TResult">The type of the resulting value.</typeparam>
        /// <param name="source">The <see cref="IEnumerable&lt;T&gt;"/> to return the first element of.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="resultSelector">A function to transform the first element into the result value. Will only be called if the sequence contains an element that passes the test specified by <paramref name="predicate"/>.</param>
        /// <param name="default">The default value to return if the sequence contains no elements.</param>
        /// <returns><paramref name="default"/> if <paramref name="source"/> is empty or if no element passes the test specified by <paramref name="predicate"/>;
        /// otherwise, the transformed first element in <paramref name="source"/> that passes the test specified by <paramref name="predicate"/>.</returns>
        public static TResult FirstOrDefault<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, Func<TSource, TResult> resultSelector, TResult @default)
        {
            using (var e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                    return @default;
                return resultSelector(e.Current);
            }
        }
    }
}
