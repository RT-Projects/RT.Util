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
        /// Returns an enumeration of <see cref="RT.Util.ObsoleteTuple.Tuple&lt;T, T&gt;"/>s containing all pairs of elements from the source <see cref="IEnumerable&lt;T&gt;"/>.
        /// For example, the input sequence 1, 2 yields the pairs [1,1], [1,2], [2,1], and [2,2].
        /// </summary>
        public static IEnumerable<RT.Util.ObsoleteTuple.Tuple<T, T>> AllPairs<T>(this IEnumerable<T> source)
        {
            return source.Join(source);
        }

        /// <summary>
        /// Returns an enumeration of <see cref="RT.Util.ObsoleteTuple.Tuple&lt;T, U&gt;"/>s containing all ordered pairs of elements from the two source <see cref="IEnumerable&lt;T&gt;"/>s.
        /// For example, [1, 2].Join(["one", "two"]) results in the tuples [1, "one"], [1, "two"], [2, "one"] and [2, "two"].
        /// </summary>
        public static IEnumerable<RT.Util.ObsoleteTuple.Tuple<T, U>> Join<T, U>(this IEnumerable<T> source, IEnumerable<U> with)
        {
            // Make sure that 'with' is evaluated only once
            U[] withArr = with.ToArray();
            foreach (T item1 in source)
                foreach (U item2 in withArr)
                    yield return new RT.Util.ObsoleteTuple.Tuple<T, U>(item1, item2);
        }

        /// <summary>
        /// Returns an enumeration of <see cref="RT.Util.ObsoleteTuple.Tuple&lt;T, T&gt;"/>s containing all unique pairs of distinct elements from the source <see cref="IEnumerable&lt;T&gt;"/>.
        /// For example, the input sequence 1, 2, 3 yields the pairs [1,2], [1,3] and [2,3] only.
        /// </summary>
        public static IEnumerable<RT.Util.ObsoleteTuple.Tuple<T, T>> UniquePairsObsolete<T>(this IEnumerable<T> source)
        {
            // Make sure that 'source' is evaluated only once
            T[] arr = source.ToArray();
            for (int i = 0; i < arr.Length - 1; i++)
                for (int j = i + 1; j < arr.Length; j++)
                    yield return new RT.Util.ObsoleteTuple.Tuple<T, T>(arr[i], arr[j]);
        }

        /// <summary>
        /// Returns an enumeration of tuples containing all unique pairs of distinct elements from the source <see cref="IEnumerable&lt;T&gt;"/>.
        /// For example, the input sequence 1, 2, 3 yields the pairs [1,2], [1,3] and [2,3] only.
        /// </summary>
        public static IEnumerable<Tuple<T, T>> UniquePairs<T>(this IEnumerable<T> source)
        {
            // Make sure that 'source' is evaluated only once
            T[] arr = source.ToArray();
            for (int i = 0; i < arr.Length - 1; i++)
                for (int j = i + 1; j < arr.Length; j++)
                    yield return new Tuple<T, T>(arr[i], arr[j]);
        }

        /// <summary>
        /// Returns an enumeration of <see cref="RT.Util.ObsoleteTuple.Tuple&lt;T, T&gt;"/>s containing all consecutive pairs of the elements.
        /// </summary>
        /// <param name="enumerable">The input enumerable.</param>
        /// <param name="closed">If true, an additional pair containing the last and first element is included. For example,
        /// if the source collection contains { 1, 2, 3, 4 } then the enumeration contains { (1, 2), (2, 3), (3, 4) } if <paramref name="closed"/>
        /// is false, and { (1, 2), (2, 3), (3, 4), (4, 1) } if <paramref name="closed"/> is true.</param>
        public static IEnumerable<RT.Util.ObsoleteTuple.Tuple<T, T>> ConsecutivePairs<T>(this IEnumerable<T> enumerable, bool closed)
        {
            var enumer = enumerable.GetEnumerator();
            bool any = enumer.MoveNext();
            if (!any) yield break;
            T first = enumer.Current;
            T last = enumer.Current;
            while (enumer.MoveNext())
            {
                yield return new RT.Util.ObsoleteTuple.Tuple<T, T>(last, enumer.Current);
                last = enumer.Current;
            }
            if (closed)
                yield return new RT.Util.ObsoleteTuple.Tuple<T, T>(last, first);
        }

        /// <summary>Sorts the elements of a sequence in ascending order.</summary>
        public static IEnumerable<T> Order<T>(this IEnumerable<T> source)
        {
            return source.OrderBy(x => x);
        }

        /// <summary>Sorts the elements of a sequence in ascending order by using a specified comparer.</summary>
        public static IEnumerable<T> Order<T>(this IEnumerable<T> source, IComparer<T> comparer)
        {
            return source.OrderBy(x => x, comparer);
        }

        /// <summary>Sorts the elements of a sequence in ascending order by using a specified comparison delegate.</summary>
        public static IEnumerable<T> OrderBy<T>(this IEnumerable<T> source, Comparison<T> comparison)
        {
            return source.OrderBy(x => x, new CustomComparer<T>(comparison));
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
        public static IEnumerable<T> Concat<T>(this IEnumerable<T> input, T element)
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
        public static IEnumerable<T> Concat<T>(this T head, IEnumerable<T> tail)
        {
            yield return head;
            foreach (var e in tail)
                yield return e;
        }

        /// <summary>
        /// This does the same as <see cref="Order&lt;T&gt;(IEnumerable&lt;T&gt;)"/>, but it is much faster if you intend to extract only the first few items using .Take().
        /// </summary>
        /// <param name="source">The sequence to be sorted.</param>
        /// <returns>The given IEnumerable&lt;T&gt; with its elements sorted progressively.</returns>
        public static IEnumerable<T> OrderLazy<T>(this IEnumerable<T> source)
        {
            return OrderLazy(source, Comparer<T>.Default);
        }

        /// <summary>
        /// This does the same as <see cref="Order&lt;T&gt;(IEnumerable&lt;T&gt;,IComparer&lt;T&gt;)"/>, but it is much faster if you intend to extract only the first few items using .Take().
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
        /// Returns all permutations of the input <see cref="IEnumerable&lt;T&gt;"/>.
        /// </summary>
        /// <param name="source">The list of items to permute.</param>
        /// <returns>A collection containing all permutations of the input <see cref="IEnumerable&lt;T&gt;"/>.</returns>
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
        /// Returns all subsequences of the input <see cref="IEnumerable&lt;T&gt;"/>.
        /// </summary>
        /// <param name="source">The sequence of items to generate subsequences of.</param>
        /// <returns>A collection containing all subsequences of the input <see cref="IEnumerable&lt;T&gt;"/>.</returns>
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
        /// <param name="default">The default value to return if the sequence contains no elements.</param>
        /// <returns><paramref name="default"/> if <paramref name="source"/> is empty;
        /// otherwise, the first element in <paramref name="source"/>.</returns>
        public static T FirstOrDefault<T>(this IEnumerable<T> source, T @default)
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
                while (true)
                {
                    if (!e.MoveNext())
                        return @default;
                    if (predicate(e.Current))
                        return e.Current;
                }
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
                while (true)
                {
                    if (!e.MoveNext())
                        return @default;
                    if (predicate(e.Current))
                        return resultSelector(e.Current);
                }
            }
        }

        /// <summary>
        /// Enumerates all pairs of values from this and the <paramref name="other"/> sequence that have the same
        /// index. The shorter sequence is padded with its type's default value to match the length of the longer sequence.
        /// For example, [1, 2, 3, 4].ZipPad(["one", "two", "three"]) enumerates [1, "one"], [2, "two"], [3, "three"], [4, null].
        /// </summary>
        public static IEnumerable<RT.Util.ObsoleteTuple.Tuple<T1, T2>> ZipPad<T1, T2>(this IEnumerable<T1> @this, IEnumerable<T2> other)
        {
            var enum1 = @this.GetEnumerator();
            var enum2 = other.GetEnumerator();
            bool more1 = enum1.MoveNext();
            bool more2 = enum2.MoveNext();
            while (more1 || more2)
            {
                yield return new RT.Util.ObsoleteTuple.Tuple<T1, T2>(more1 ? enum1.Current : default(T1), more2 ? enum2.Current : default(T2));
                if (more1)
                    more1 = enum1.MoveNext();
                if (more2)
                    more2 = enum2.MoveNext();
            }
        }

        /// <summary>
        /// Enumerates all pairs of values from this and the <paramref name="other"/> sequence that have the same
        /// index. The longer sequence is truncated to match the length of the shorter sequence.
        /// For example, [1, 2, 3, 4].ZipTruncate(["one", "two", "three"]) enumerates [1, "one"], [2, "two"], [3, "three"].
        /// </summary>
        public static IEnumerable<RT.Util.ObsoleteTuple.Tuple<T1, T2>> ZipTruncate<T1, T2>(this IEnumerable<T1> @this, IEnumerable<T2> other)
        {
            var enum1 = @this.GetEnumerator();
            var enum2 = other.GetEnumerator();
            bool more1 = enum1.MoveNext();
            bool more2 = enum2.MoveNext();
            while (more1 && more2)
            {
                yield return new RT.Util.ObsoleteTuple.Tuple<T1, T2>(enum1.Current, enum2.Current);
                more1 = enum1.MoveNext();
                more2 = enum2.MoveNext();
            }
        }

        /// <summary>
        /// Enumerates all pairs of values from this and the <paramref name="other"/> sequence that have the same
        /// index. The second sequence is either padded or truncated to match the first sequence's length.
        /// </summary>
        public static IEnumerable<RT.Util.ObsoleteTuple.Tuple<T1, T2>> Zip<T1, T2>(this IEnumerable<T1> @this, IEnumerable<T2> other)
        {
            var enum1 = @this.GetEnumerator();
            var enum2 = other.GetEnumerator();
            while (enum1.MoveNext())
            {
                T2 elem = enum2.MoveNext() ? enum2.Current : default(T2);
                yield return new RT.Util.ObsoleteTuple.Tuple<T1, T2>(enum1.Current, elem);
            }
        }

        /// <summary>
        /// Returns the index of the first element in this <paramref name="enumerable"/> satisfying
        /// the specified <paramref name="condition"/>. If no such elements are found, returns -1.
        /// </summary>
        public static int IndexOf<T>(this IEnumerable<T> enumerable, Func<T, bool> condition)
        {
            int index = 0;
            foreach (var v in enumerable)
            {
                if (condition(v))
                    return index;
                index++;
            }
            return -1;
        }

        /// <summary>Returns the first element from the input sequence for which the value selector returns the smallest value.</summary>
        public static T MinElement<T>(this IEnumerable<T> source, Func<T, int> valueSelector)
        {
            using (var enumerator = source.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    throw new InvalidOperationException("source contains no elements.");
                T minElem = enumerator.Current;
                int minValue = valueSelector(minElem);
                while (enumerator.MoveNext())
                {
                    int value = valueSelector(enumerator.Current);
                    if (value < minValue)
                    {
                        minValue = value;
                        minElem = enumerator.Current;
                    }
                }
                return minElem;
            }
        }

        /// <summary>
        /// Enumerates the items of this collection, skipping the last <paramref name="count"/> items. Note that the
        /// memory usage of this method is proportional to <paramref name="count"/>, but the source collection is
        /// only enumerated once, and in a lazy fashion. Also, enumerating the first item will take longer than
        /// enumerating subsequent items.
        /// </summary>
        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source, int count)
        {
            var queue = new T[count];
            int headtail = 0; // tail while we're still collecting, both head & tail afterwards because the queue becomes completely full
            int collected = 0;

            foreach (var item in source)
            {
                if (collected < count)
                {
                    queue[headtail] = item;
                    headtail++;
                    collected++;
                }
                else
                {
                    if (headtail == count) headtail = 0;
                    yield return queue[headtail];
                    queue[headtail] = item;
                    headtail++;
                }
            }
        }

        /// <summary>
        /// Returns a collection containing only the last <paramref name="count"/> items of the input collection.
        /// This method enumerates the entire collection to the end once before returning. Note also that
        /// the memory usage of this method is proportional to <paramref name="count"/>.
        /// </summary>
        public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int count)
        {
            var queue = new Queue<T>(count + 1);

            foreach (var item in source)
            {
                if (queue.Count == count)
                    queue.Dequeue();
                queue.Enqueue(item);
            }

            return queue;
        }

        /// <summary>Returns true if and only if the input collection begins with the specified collection.</summary>
        public static bool StartsWith<T>(this IEnumerable<T> source, IEnumerable<T> sequence)
        {
            return StartsWith<T>(source, sequence, EqualityComparer<T>.Default);
        }

        /// <summary>Returns true if and only if the input collection begins with the specified collection.</summary>
        public static bool StartsWith<T>(this IEnumerable<T> source, IEnumerable<T> sequence, IEqualityComparer<T> comparer)
        {
            using (var sourceEnum = source.GetEnumerator())
            using (var seqEnum = sequence.GetEnumerator())
            {
                while (true)
                {
                    if (!seqEnum.MoveNext())
                        return true;
                    if (!sourceEnum.MoveNext())
                        return false;
                    if (!comparer.Equals(sourceEnum.Current, seqEnum.Current))
                        return false;
                }
            }
        }
    }
}
