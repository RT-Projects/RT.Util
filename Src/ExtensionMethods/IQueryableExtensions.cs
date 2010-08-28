using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RT.Util.Collections;
using System.Linq.Expressions;

namespace RT.Util.ExtensionMethods
{
    /// <summary>
    /// Provides extension methods on the collection type.
    /// </summary>
    public static class IQueryableExtensions
    {
        /// <summary>
        /// Returns an enumeration of tuples containing all pairs of elements from the source collection.
        /// For example, the input sequence 1, 2 yields the pairs [1,1], [1,2], [2,1], and [2,2].
        /// </summary>
        public static IQueryable<Tuple<T, T>> AllPairs<T>(this IQueryable<T> source)
        {
            return source.SelectMany(item1 => source.Select(item2 => new Tuple<T, T>(item1, item2)));
        }

        /// <summary>
        /// Returns an enumeration of <see cref="Tuple&lt;T, U&gt;"/>s containing all ordered pairs of elements from the two source collections.
        /// For example, [1, 2].Join(["one", "two"]) results in the tuples [1, "one"], [1, "two"], [2, "one"] and [2, "two"].
        /// </summary>
        public static IQueryable<Tuple<T, U>> Join<T, U>(this IQueryable<T> source, IQueryable<U> with)
        {
            return source.SelectMany(item1 => with.Select(item2 => new Tuple<T, U>(item1, item2)));
        }

        /// <summary>
        /// Returns an enumeration of tuples containing all unique pairs of distinct elements from the source collection.
        /// For example, the input sequence 1, 2, 3 yields the pairs [1,2], [1,3] and [2,3] only.
        /// </summary>
        /// <remarks>Warning: This method does not work with IQToolkit.</remarks>
        public static IQueryable<Tuple<T, T>> UniquePairs<T>(this IQueryable<T> source)
        {
            return source.SelectMany((item1, index) => source.Take(index + 1).Select(item2 => new Tuple<T, T>(item1, item2)));
        }

        /// <summary>
        /// Returns an enumeration of tuples containing all consecutive pairs of the elements.
        /// </summary>
        /// <param name="source">The input enumerable.</param>
        /// <param name="closed">If true, an additional pair containing the last and first element is included. For example,
        /// if the source collection contains { 1, 2, 3, 4 } then the enumeration contains { (1, 2), (2, 3), (3, 4) } if <paramref name="closed"/>
        /// is false, and { (1, 2), (2, 3), (3, 4), (4, 1) } if <paramref name="closed"/> is true.</param>
        /// <remarks>Warning: This method does not work with IQToolkit.</remarks>
        public static IQueryable<Tuple<T, T>> ConsecutivePairs<T>(this IQueryable<T> source, bool closed)
        {
            if (closed)
                return source.Select((item, index) => new Tuple<T, T>(item, source.Concat(source.First()).Skip(index + 1).First()));
            else
                return source.Select((item, index) => new Tuple<T, T>(item, source.Skip(index + 1).First()));
        }

        /// <summary>
        /// Returns an enumeration of the specified enumerable in sorted order.
        /// </summary>
        public static IOrderedQueryable<T> Order<T>(this IQueryable<T> source)
        {
            return source.OrderBy(x => x);
        }

        /// <summary>
        /// Returns an enumeration of the specified enumerable in sorted order.
        /// </summary>
        public static IOrderedQueryable<T> Order<T>(this IQueryable<T> source, IComparer<T> comparison)
        {
            return source.OrderBy(x => x, comparison);
        }

        /// <summary>
        /// Adds a single element to the end of an IQueryable.
        /// </summary>
        /// <typeparam name="T">Type of enumerable to return.</typeparam>
        /// <returns>IQueryable containing all the input elements, followed by the specified additional element.</returns>
        public static IQueryable<T> Concat<T>(this IQueryable<T> input, T element)
        {
            return input.Concat(new[] { element });
        }

        /// <summary>
        /// Returns the first element of a sequence, or a default value if the sequence contains no elements.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The collection to return the first element of.</param>
        /// <param name="default">The default value to return if the sequence contains no elements.</param>
        /// <returns><paramref name="default"/> if <paramref name="source"/> is empty;
        /// otherwise, the first element in <paramref name="source"/>.</returns>
        public static T FirstOrDefault<T>(this IQueryable<T> source, T @default)
        {
            var arr = source.Take(1).ToArray();
            return arr.Length == 1 ? arr[0] : @default;
        }

        /// <summary>
        /// Returns the first element of a sequence, or a default value if the sequence contains no elements.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The collection to return the first element of.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="default">The default value to return if the sequence contains no elements.</param>
        /// <returns><paramref name="default"/> if <paramref name="source"/> is empty or if no element passes the test specified by <paramref name="predicate"/>;
        /// otherwise, the first element in <paramref name="source"/> that passes the test specified by <paramref name="predicate"/>.</returns>
        public static T FirstOrDefault<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, T @default)
        {
            var arr = source.Where(predicate).Take(1).ToArray();
            return arr.Length == 1 ? arr[0] : @default;
        }

        /// <summary>
        /// Returns the first element of a sequence, or a default value if the sequence contains no elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TResult">The type of the resulting value.</typeparam>
        /// <param name="source">The collection to return the first element of.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="resultSelector">A function to transform the first element into the result value. Will only be called if the sequence contains an element that passes the test specified by <paramref name="predicate"/>.</param>
        /// <param name="default">The default value to return if the sequence contains no elements.</param>
        /// <returns><paramref name="default"/> if <paramref name="source"/> is empty or if no element passes the test specified by <paramref name="predicate"/>;
        /// otherwise, the transformed first element in <paramref name="source"/> that passes the test specified by <paramref name="predicate"/>.</returns>
        public static TResult FirstOrDefault<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, Func<TSource, TResult> resultSelector, TResult @default)
        {
            var arr = source.Where(predicate).Select(resultSelector).Take(1).ToArray();
            return arr.Length == 1 ? arr[0] : @default;
        }
    }
}
