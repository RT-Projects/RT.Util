using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RT.Util.Consoles;

namespace RT.Util.ExtensionMethods
{
    /// <summary>
    /// Provides extension methods on the <see cref="IEnumerable&lt;T&gt;"/> type.
    /// </summary>
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Returns an enumeration of tuples containing all pairs of elements from the source collection.
        /// For example, the input sequence 1, 2 yields the pairs [1,1], [1,2], [2,1], and [2,2].
        /// </summary>
        public static IEnumerable<Tuple<T, T>> AllPairs<T>(this IEnumerable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            // Make sure that ‘source’ is evaluated only once
            var sourceArr = source as IList<T> ?? source.ToArray();
            return sourceArr.SelectMany(item1 => sourceArr.Select(item2 => new Tuple<T, T>(item1, item2)));
        }

        /// <summary>
        /// Returns an enumeration of objects computed from all pairs of elements from the source collection.
        /// </summary>
        public static IEnumerable<TResult> AllPairs<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TSource, TResult> selector)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            // Make sure that ‘source’ is evaluated only once
            var sourceArr = source as IList<TSource> ?? source.ToArray();
            return sourceArr.SelectMany(item1 => sourceArr.Select(item2 => selector(item1, item2)));
        }

        /// <summary>
        /// Returns an enumeration of tuples containing all unique pairs of distinct elements from the source collection.
        /// For example, the input sequence 1, 2, 3 yields the pairs [1,2], [1,3] and [2,3] only.
        /// </summary>
        public static IEnumerable<Tuple<T, T>> UniquePairs<T>(this IEnumerable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            return uniquePairsIterator(source);
        }
        private static IEnumerable<Tuple<T, T>> uniquePairsIterator<T>(IEnumerable<T> source)
        {
            // Make sure that ‘source’ is evaluated only once
            IList<T> arr = source as IList<T> ?? source.ToArray();
            for (int i = 0; i < arr.Count - 1; i++)
                for (int j = i + 1; j < arr.Count; j++)
                    yield return new Tuple<T, T>(arr[i], arr[j]);
        }

        /// <summary>
        /// Returns an enumeration of tuples containing all consecutive pairs of the elements.
        /// </summary>
        /// <param name="source">The input enumerable.</param>
        /// <param name="closed">If true, an additional pair containing the last and first element is included. For example,
        /// if the source collection contains { 1, 2, 3, 4 } then the enumeration contains { (1, 2), (2, 3), (3, 4) } if <paramref name="closed"/>
        /// is false, and { (1, 2), (2, 3), (3, 4), (4, 1) } if <paramref name="closed"/> is true.</param>
        public static IEnumerable<Tuple<T, T>> ConsecutivePairs<T>(this IEnumerable<T> source, bool closed)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            return consecutivePairsIterator(source, closed);
        }
        private static IEnumerable<Tuple<T, T>> consecutivePairsIterator<T>(IEnumerable<T> source, bool closed)
        {
            using (var enumer = source.GetEnumerator())
            {
                bool any = enumer.MoveNext();
                if (!any) yield break;
                T first = enumer.Current;
                T last = enumer.Current;
                while (enumer.MoveNext())
                {
                    yield return new Tuple<T, T>(last, enumer.Current);
                    last = enumer.Current;
                }
                if (closed)
                    yield return new Tuple<T, T>(last, first);
            }
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
            if (source == null)
                throw new ArgumentNullException("source");
            if (comparison == null)
                throw new ArgumentNullException("comparison");
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
            if (splitWhat == null)
                throw new ArgumentNullException("splitWhat");
            if (splitWhere == null)
                throw new ArgumentNullException("splitWhere");
            return splitIterator(splitWhat, splitWhere);
        }
        private static IEnumerable<IEnumerable<T>> splitIterator<T>(IEnumerable<T> splitWhat, Func<T, bool> splitWhere)
        {
            int prevIndex = 0;
            foreach (var index in splitWhat.Select((elem, ind) => new { e = elem, i = ind }).Where(x => splitWhere(x.e)))
            {
                yield return splitWhat.Skip(prevIndex).Take(index.i - prevIndex);
                prevIndex = index.i + 1;
            }
            yield return splitWhat.Skip(prevIndex);
        }

        /// <summary>Adds a single element to the end of an IEnumerable.</summary>
        /// <typeparam name="T">Type of enumerable to return.</typeparam>
        /// <returns>IEnumerable containing all the input elements, followed by the specified additional element.</returns>
        public static IEnumerable<T> Concat<T>(this IEnumerable<T> source, T element)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            return concatIterator(element, source, false);
        }

        /// <summary>Adds a single element to the start of an IEnumerable.</summary>
        /// <typeparam name="T">Type of enumerable to return.</typeparam>
        /// <returns>IEnumerable containing the specified additional element, followed by all the input elements.</returns>
        public static IEnumerable<T> Concat<T>(this T head, IEnumerable<T> tail)
        {
            if (tail == null)
                throw new ArgumentNullException("tail");
            return concatIterator(head, tail, true);
        }

        private static IEnumerable<T> concatIterator<T>(T extraElement, IEnumerable<T> source, bool insertAtStart)
        {
            if (insertAtStart)
                yield return extraElement;
            foreach (var e in source)
                yield return e;
            if (!insertAtStart)
                yield return extraElement;
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
            if (source == null)
                throw new ArgumentNullException("source");
            if (comparer == null)
                throw new ArgumentNullException("comparer");
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
            if (source == null)
                throw new ArgumentNullException("source");
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
            if (source == null)
                throw new ArgumentNullException("source");
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
            if (source == null)
                throw new ArgumentNullException("source");
            using (var e = source.GetEnumerator())
            {
                if (e.MoveNext())
                    return e.Current;
                return @default;
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
            if (source == null)
                throw new ArgumentNullException("source");
            if (predicate == null)
                throw new ArgumentNullException("predicate");
            using (var e = source.GetEnumerator())
            {
                while (e.MoveNext())
                    if (predicate(e.Current))
                        return e.Current;
                return @default;
            }
        }

        /// <summary>Returns the first element of a sequence, or a default value if the sequence contains no elements.</summary>
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
            if (source == null)
                throw new ArgumentNullException("source");
            if (predicate == null)
                throw new ArgumentNullException("predicate");
            if (resultSelector == null)
                throw new ArgumentNullException("resultSelector");
            using (var e = source.GetEnumerator())
            {
                while (e.MoveNext())
                    if (predicate(e.Current))
                        return resultSelector(e.Current);
                return @default;
            }
        }

        /// <summary>Returns the only element of a sequence that satisfies a specified condition, the type's default value if no such element exists,
        /// and throws an exception if more than one such element exists.</summary>
        /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The <see cref="IEnumerable&lt;T&gt;"/> to return the one element of.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>Returns the type's default value if no item matches the predicate, or the only item if there is exactly one.
        /// Otherwise, throws InvalidOperationException.</returns>
        public static T AtMostOne<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (predicate == null)
                throw new ArgumentNullException("predicate");
            using (var e = source.Where(predicate).GetEnumerator())
            {
                if (!e.MoveNext())
                    return default(T);
                var value = e.Current;
                if (e.MoveNext())
                    throw new InvalidOperationException("The sequence contained more than one matching element.");
                return value;
            }
        }

        /// <summary>
        /// Returns the index of the first element in this <paramref name="source"/> satisfying
        /// the specified <paramref name="predicate"/>. If no such elements are found, returns -1.
        /// </summary>
        public static int IndexOf<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (predicate == null)
                throw new ArgumentNullException("predicate");
            int index = 0;
            foreach (var v in source)
            {
                if (predicate(v))
                    return index;
                index++;
            }
            return -1;
        }

        /// <summary>
        /// Returns the index of the first element in this <paramref name="source"/> that is equal to
        /// the specified <paramref name="element"/> as determined by the specified <paramref name="comparer"/>.
        /// If no such elements are found, returns -1.
        /// </summary>
        public static int IndexOf<T>(this IEnumerable<T> source, T element, IEqualityComparer<T> comparer = null)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (comparer == null)
                comparer = EqualityComparer<T>.Default;
            int index = 0;
            foreach (var v in source)
            {
                if (comparer.Equals(v, element))
                    return index;
                index++;
            }
            return -1;
        }

        /// <summary>Returns the first element from the input sequence for which the value selector returns the smallest value.</summary>
        /// <exception cref="InvalidOperationException">The input collection is empty.</exception>
        public static T MinElement<T, TValue>(this IEnumerable<T> source, Func<T, TValue> valueSelector) where TValue : IComparable<TValue>
        {
            return minMaxElement(source, valueSelector, true, true, default(T));
        }

        /// <summary>Returns the first element from the input sequence for which the value selector returns the smallest value, or a default value if the collection is empty.</summary>
        public static T MinElementOrDefault<T, TValue>(this IEnumerable<T> source, Func<T, TValue> valueSelector, T defaultValue = default(T)) where TValue : IComparable<TValue>
        {
            return minMaxElement(source, valueSelector, true, false, defaultValue);
        }

        /// <summary>Returns the first element from the input sequence for which the value selector returns the largest value.</summary>
        /// <exception cref="InvalidOperationException">The input collection is empty.</exception>
        public static T MaxElement<T, TValue>(this IEnumerable<T> source, Func<T, TValue> valueSelector) where TValue : IComparable<TValue>
        {
            return minMaxElement(source, valueSelector, false, true, default(T));
        }

        /// <summary>Returns the first element from the input sequence for which the value selector returns the largest value, or a default value if the collection is empty.</summary>
        public static T MaxElementOrDefault<T, TValue>(this IEnumerable<T> source, Func<T, TValue> valueSelector, T defaultValue = default(T)) where TValue : IComparable<TValue>
        {
            return minMaxElement(source, valueSelector, false, false, defaultValue);
        }

        private static T minMaxElement<T, TValue>(IEnumerable<T> source, Func<T, TValue> valueSelector, bool min, bool doThrow, T defaultValue) where TValue : IComparable<TValue>
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (valueSelector == null)
                throw new ArgumentNullException("valueSelector");
            using (var enumerator = source.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                {
                    if (doThrow)
                        throw new InvalidOperationException("source contains no elements.");
                    return defaultValue;
                }
                T minMaxElem = enumerator.Current;
                TValue minMaxValue = valueSelector(minMaxElem);
                while (enumerator.MoveNext())
                {
                    TValue value = valueSelector(enumerator.Current);
                    if (min ? (value.CompareTo(minMaxValue) < 0) : (value.CompareTo(minMaxValue) > 0))
                    {
                        minMaxValue = value;
                        minMaxElem = enumerator.Current;
                    }
                }
                return minMaxElem;
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
            if (source == null)
                throw new ArgumentNullException("source");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", "count cannot be negative.");
            if (count == 0)
                return source;
            return skipLastIterator(source, count);
        }
        private static IEnumerable<T> skipLastIterator<T>(IEnumerable<T> source, int count)
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
            if (source == null)
                throw new ArgumentNullException("source");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", "count cannot be negative.");
            if (count == 0)
                return new T[0];

            var queue = new Queue<T>(count + 1);
            foreach (var item in source)
            {
                if (queue.Count == count)
                    queue.Dequeue();
                queue.Enqueue(item);
            }
            return queue.AsEnumerable();
        }

        /// <summary>Returns true if and only if the input collection begins with the specified collection.</summary>
        public static bool StartsWith<T>(this IEnumerable<T> source, IEnumerable<T> sequence)
        {
            return StartsWith<T>(source, sequence, EqualityComparer<T>.Default);
        }

        /// <summary>Returns true if and only if the input collection begins with the specified collection.</summary>
        public static bool StartsWith<T>(this IEnumerable<T> source, IEnumerable<T> sequence, IEqualityComparer<T> comparer)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (sequence == null)
                throw new ArgumentNullException("sequence");
            if (comparer == null)
                throw new ArgumentNullException("comparer");

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

        /// <summary>Creates a <see cref="Queue&lt;T&gt;"/> from an enumerable collection.</summary>
        public static Queue<T> ToQueue<T>(this IEnumerable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            return new Queue<T>(source);
        }

        /// <summary>Creates a <see cref="Stack&lt;T&gt;"/> from an enumerable collection.</summary>
        public static Stack<T> ToStack<T>(this IEnumerable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            return new Stack<T>(source);
        }

        /// <summary>Creates a <see cref="HashSet&lt;T&gt;"/> from an enumerable collection.</summary>
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            return new HashSet<T>(source);
        }

        /// <summary>Returns a collection of integer containing the indexes at which the elements of the source collection match the given predicate.</summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="source">The source collection whose elements are tested using <paramref name="predicate"/>.</param>
        /// <param name="predicate">The predicate against which the elements of <paramref name="source"/> are tested.</param>
        /// <returns>A collection containing the zero-based indexes of all the matching elements, in increasing order.</returns>
        public static IEnumerable<int> SelectIndexWhere<T>(this IEnumerable<T> source, Predicate<T> predicate)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (predicate == null)
                throw new ArgumentNullException("predicate");
            return selectIndexWhereIterator(source, predicate);
        }
        private static IEnumerable<int> selectIndexWhereIterator<T>(this IEnumerable<T> source, Predicate<T> predicate)
        {
            int i = 0;
            using (var e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    if (predicate(e.Current))
                        yield return i;
                    i++;
                }
            }
        }

        /// <summary>Transforms every element of an input collection using two selector functions and returns a collection containing all the results.</summary>
        /// <typeparam name="TSource">Type of the elements in the source collection.</typeparam>
        /// <typeparam name="TResult">Type of the results of the selector functions.</typeparam>
        /// <param name="source">Input collection to transform.</param>
        /// <param name="selector1">First selector function.</param>
        /// <param name="selector2">Second selector function.</param>
        /// <returns>A collection containing the transformed elements from both selectors, thus containing twice as many elements as the original collection.</returns>
        public static IEnumerable<TResult> SelectTwo<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector1, Func<TSource, TResult> selector2)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (selector1 == null)
                throw new ArgumentNullException("selector1");
            if (selector2 == null)
                throw new ArgumentNullException("selector2");
            return selectTwoIterator(source, selector1, selector2);
        }
        private static IEnumerable<TResult> selectTwoIterator<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, TResult> selector1, Func<TSource, TResult> selector2)
        {
            foreach (var elem in source)
            {
                yield return selector1(elem);
                yield return selector2(elem);
            }
        }

        /// <summary>
        /// <para>Turns all elements in the enumerable to strings and joins them using the specified string
        /// as the separator and the specified prefix and suffix for each string.</para>
        /// <example>
        ///     <code>
        ///         var a = (new[] { "Paris", "London", "Tokyo" }).JoinString("[", "]", ", ");
        ///         // a contains "[Paris], [London], [Tokyo]"
        ///     </code>
        /// </example>
        /// </summary>
        public static string JoinString<T>(this IEnumerable<T> values, string separator = null, string prefix = null, string suffix = null)
        {
            if (values == null)
                throw new ArgumentNullException("values");

            using (var enumerator = values.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    return "";

                // Optimise the case where there is only one element
                var one = enumerator.Current;
                if (!enumerator.MoveNext())
                    return prefix + one + suffix;

                // Optimise the case where there are only two elements
                var two = enumerator.Current;
                if (!enumerator.MoveNext())
                {
                    // Optimise the (common) case where there is no prefix/suffix; this prevents an array allocation when calling string.Concat()
                    if (prefix == null && suffix == null)
                        return one + separator + two;
                    return prefix + one + suffix + separator + prefix + two + suffix;
                }

                StringBuilder sb = new StringBuilder()
                    .Append(prefix).Append(one).Append(suffix).Append(separator)
                    .Append(prefix).Append(two).Append(suffix).Append(separator)
                    .Append(prefix).Append(enumerator.Current).Append(suffix);
                while (enumerator.MoveNext())
                    sb.Append(separator).Append(prefix).Append(enumerator.Current).Append(suffix);
                return sb.ToString();
            }
        }

        /// <summary>
        /// <para>Turns all elements in the enumerable to strings and joins them using the specified separators.</para>
        /// <example>
        ///     <code>
        ///         var a = (new[] { "Paris", "London", "Tokyo" }).JoinStringWithAnd(", ", " and ");
        ///         // a contains "Paris, London and Tokyo"
        ///     </code>
        /// </example>
        /// </summary>
        public static string JoinStringWithAnd<T>(this IEnumerable<T> values, string commaSeparator, string andSeparator)
        {
            if (values == null)
                throw new ArgumentNullException("values");

            using (var enumerator = values.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    return "";

                // Optimise the case where there is only one element
                var one = enumerator.Current;
                if (!enumerator.MoveNext())
                    return one.ToString();

                // Optimise the case where there are only two elements
                var two = enumerator.Current;
                if (!enumerator.MoveNext())
                    return one + andSeparator + two;

                StringBuilder sb = new StringBuilder().Append(one).Append(commaSeparator).Append(two);
                var prev = enumerator.Current;
                while (enumerator.MoveNext())
                {
                    sb.Append(commaSeparator).Append(prev);
                    prev = enumerator.Current;
                }
                sb.Append(andSeparator).Append(prev);
                return sb.ToString();
            }
        }

        /// <summary>Equivalent to <see cref="JoinString{T}(IEnumerable{T},string,string,string)"/>, but for <see cref="ConsoleColoredString"/>s.</summary>
        public static ConsoleColoredString JoinColoredString<T>(this IEnumerable<T> values, ConsoleColoredString separator = null, ConsoleColoredString prefix = null, ConsoleColoredString suffix = null, ConsoleColor defaultColor = ConsoleColor.Gray)
        {
            if (values == null)
                throw new ArgumentNullException("values");

            using (var enumerator = values.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    return ConsoleColoredString.Empty;

                var list = new List<ConsoleColoredString>(values is ICollection<T> ? ((ICollection<T>) values).Count * 4 : 8);
                bool first = true;
                do
                {
                    if (!first && separator != null)
                        list.Add(separator);
                    first = false;
                    if (prefix != null)
                        list.Add(prefix);
                    if (enumerator.Current != null)
                        list.Add(enumerator.Current.ToConsoleColoredString(defaultColor));
                    if (suffix != null)
                        list.Add(suffix);
                }
                while (enumerator.MoveNext());
                return new ConsoleColoredString(list);
            }
        }

        /// <summary>Inserts the specified item in between each element in the input collection.</summary>
        /// <param name="source">The input collection.</param>
        /// <param name="extraElement">The element to insert between each consecutive pair of elements in the input collection.</param>
        /// <returns>A collection containing the original collection with the extra element inserted.
        /// For example, new[] { 1, 2, 3 }.InsertBetween(0) returns { 1, 0, 2, 0, 3 }.</returns>
        public static IEnumerable<T> InsertBetween<T>(this IEnumerable<T> source, T extraElement)
        {
            return source.SelectMany(val => new[] { extraElement, val }).Skip(1);
        }

        /// <summary>Inserts the <paramref name="comma"/> item in between each element in the input collection except between the second-last and last, where it inserts <paramref name="and"/> instead.</summary>
        /// <param name="source">The input collection.</param>
        /// <param name="comma">The element to insert between each consecutive pair of elements in the input collection except between the second-last and last.</param>
        /// <param name="and">The element to insert between the second-last and last element of the input collection.</param>
        /// <returns>A collection containing the original collection with the extra element inserted.
        /// For example, new[] { "a", "b", "c" }.InsertBetweenWithAnd(", ", " and ") returns { "a", ", ", "b", " and ", "c" }.</returns>
        public static IEnumerable<T> InsertBetweenWithAnd<T>(this IEnumerable<T> source, T comma, T and)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            return insertBetweenWithAndIterator(source, comma, and);
        }

        private static IEnumerable<T> insertBetweenWithAndIterator<T>(IEnumerable<T> source, T comma, T and)
        {
            using (var enumerator = source.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    yield break;
                yield return enumerator.Current;
                if (!enumerator.MoveNext())
                    yield break;

                var prev = enumerator.Current;
                while (enumerator.MoveNext())
                {
                    yield return comma;
                    yield return prev;
                    prev = enumerator.Current;
                }
                yield return and;
                yield return prev;
            }
        }

        /// <summary>Determines whether all the input sequences are equal according to SequenceEquals.</summary>
        public static bool AllSequencesEqual<T>(this IEnumerable<IEnumerable<T>> sources)
        {
            using (var e = sources.GetEnumerator())
            {
                if (!e.MoveNext())
                    return true;
                var firstSequence = e.Current;
                while (e.MoveNext())
                    if (!firstSequence.SequenceEqual(e.Current))
                        return false;
                return true;
            }
        }

        /// <summary>Splits a collection into chunks of equal size. The last chunk may be smaller than <paramref name="chunkSize"/>, but all chunks, if any, will contain at least one item.</summary>
        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> source, int chunkSize)
        {
            if (chunkSize <= 0)
                throw new ArgumentException("chunkSize must be greater than zero.", "chunkSize");
            return splitIterator(source, chunkSize);
        }
        private static IEnumerable<IEnumerable<T>> splitIterator<T>(this IEnumerable<T> source, int chunkSize)
        {
            var list = new List<T>(chunkSize);
            foreach (var item in source)
            {
                list.Add(item);
                if (list.Count == chunkSize)
                {
                    yield return list;
                    list = new List<T>(chunkSize);
                }
            }
            if (list.Count > 0)
                yield return list;
        }
    }
}
