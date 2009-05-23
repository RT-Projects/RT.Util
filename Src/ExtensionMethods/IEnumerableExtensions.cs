﻿using System;
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
        /// <returns></returns>
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
        /// This does the same as .Order(), but it uses HeapSort instead of QuickSort.
        /// This is faster if you intend to extract only the first few items using .Take().
        /// </summary>
        /// <param name="source">The sequence to be sorted.</param>
        /// <returns>The given IEnumerable&lt;T&gt; with its elements sorted progressively.</returns>
        public static IEnumerable<T> OrderTake<T>(this IEnumerable<T> source)
        {
            return OrderTake(source, Comparer<T>.Default);
        }

        /// <summary>
        /// This does the same as .Order(), but it uses HeapSort instead of QuickSort.
        /// This is faster if you intend to extract only the first few items using .Take().
        /// </summary>
        /// <param name="source">The sequence to be sorted.</param>
        /// <param name="comparer">An instance of <see cref="IComparer&lt;T&gt;"/> specifying the comparison to use on the items.</param>
        /// <returns>The given IEnumerable&lt;T&gt; with its elements sorted progressively.</returns>
        public static IEnumerable<T> OrderTake<T>(this IEnumerable<T> source, IComparer<T> comparer)
        {
            return new MyOrderedEnumerable<T>(source, comparer);
        }

        /// <summary>Used by <see cref="MyOrderedEnumerable&lt;T&gt;"/>,
        /// and thus indirectly by <see cref="IEnumerableExtensions.OrderTake&lt;T&gt;(IEnumerable&lt;T&gt;)"/>.</summary>
        private class MyOrderedEnumerator<T> : IEnumerator<T>
        {
            private T[] _heap;
            private T _current;
            private int _heapSize;
            private IComparer<T> _comparer;
            public MyOrderedEnumerator(IEnumerable<T> source, IComparer<T> comparer)
            {
                _heap = source.ToArray();
                _comparer = comparer;
                Reset();
            }

            public T Current { get { return _current; } }
            object System.Collections.IEnumerator.Current { get { return _current; } }
            public void Dispose() { }

            public bool MoveNext()
            {
                if (_heapSize < 1)
                    return false;
                _current = _heap[0];
                _heapSize--;
                if (_heapSize > 0)
                {
                    T t = _heap[_heapSize];
                    _heap[_heapSize] = _current;
                    _heap[0] = t;
                    int index = 0;
                    while (index < _heapSize / 2)
                        index = heapifyElement(index);
                }
                return true;
            }

            public void Reset()
            {
                _heapSize = _heap.Length;
                for (int i = _heapSize / 2 - 1; i >= 0; i--)
                {
                    var index = i;
                    while (index < _heapSize / 2)
                        index = heapifyElement(index);
                }
            }

            private int heapifyElement(int index)
            {
                if (2 * index + 2 > _heapSize)
                    return _heapSize;
                int compareIndex;
                if (2 * index + 2 == _heapSize)
                    compareIndex = 2 * index + 1;
                else
                    compareIndex = 2 * index + (_comparer.Compare(_heap[2 * index + 1], _heap[2 * index + 2]) < 0 ? 1 : 2);
                if (_comparer.Compare(_heap[index], _heap[compareIndex]) > 0)
                {
                    T t = _heap[index];
                    _heap[index] = _heap[compareIndex];
                    _heap[compareIndex] = t;
                    return compareIndex;
                }
                return _heapSize;
            }
        }

        /// <summary>Used by <see cref="IEnumerableExtensions.OrderTake&lt;T&gt;(IEnumerable&lt;T&gt;)"/>.</summary>
        private class MyOrderedEnumerable<T> : IEnumerable<T>
        {
            private IEnumerable<T> _original;
            private IComparer<T> _comparer;

            public MyOrderedEnumerable(IEnumerable<T> original, IComparer<T> comparer)
            {
                _original = original;
                _comparer = comparer;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return new MyOrderedEnumerator<T>(_original, _comparer);
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
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
    }
}
