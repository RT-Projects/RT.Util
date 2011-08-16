using System;
using System.Collections.Generic;

namespace RT.Util
{
    /// <summary>Encapsulates an IComparer&lt;T&gt; that uses a comparison function provided as a delegate.</summary>
    /// <typeparam name="T">The type of elements to be compared.</typeparam>
    public sealed class CustomComparer<T> : IComparer<T>
    {
        private Comparison<T> _comparison;
        /// <summary>Constructor.</summary>
        /// <param name="comparison">Provides the comparison function for this comparer.</param>
        public CustomComparer(Comparison<T> comparison) { _comparison = comparison; }
        /// <summary>Compares two elements.</summary>
        /// <remarks>This method implements <see cref="IComparer&lt;T&gt;.Compare(T,T)"/>.</remarks>
        public int Compare(T x, T y) { return _comparison(x, y); }

        /// <summary>Creates and returns a CustomComparer which compares items by comparing the results of a selector function.</summary>
        /// <param name="selector">Function selecting the actual value to be compared.</param>
        public static CustomComparer<T> By<TBy>(Func<T, TBy> selector) where TBy : IComparable<TBy>
        {
            return new CustomComparer<T>((a, b) => selector(a).CompareTo(selector(b)));
        }

        /// <summary>Creates and returns a CustomComparer which compares items by comparing the results of a selector function.</summary>
        /// <param name="selector">Function selecting the actual value to be compared.</param>
        /// <param name="selectedComparer">Comparer to use for comparing the results of the selector function.</param>
        public static CustomComparer<T> By<TBy>(Func<T, TBy> selector, IComparer<TBy> selectedComparer)
        {
            return new CustomComparer<T>((a, b) => selectedComparer.Compare(selector(a), selector(b)));
        }

        /// <summary>Creates and returns a CustomComparer which compares items by comparing the results of a selector function.</summary>
        /// <param name="selector">Function selecting the actual value to be compared.</param>
        /// <param name="selectedComparison">Comparison to use for comparing the results of the selector function.</param>
        public static CustomComparer<T> By<TBy>(Func<T, TBy> selector, Comparison<TBy> selectedComparison)
        {
            return new CustomComparer<T>((a, b) => selectedComparison(selector(a), selector(b)));
        }

        /// <summary>Creates and returns a CustomComparer which compares items by comparing the results of a string selector function, ignoring case.</summary>
        /// <param name="selector">Function selecting the actual value to be compared.</param>
        public static CustomComparer<T> ByStringNoCase(Func<T, string> selector)
        {
            return new CustomComparer<T>((a, b) => StringComparer.OrdinalIgnoreCase.Compare(selector(a), selector(b)));
        }

        /// <summary>Creates and returns a CustomComparer which uses the current comparer first, and if the current comparer says the
        /// items are equal, further compares items by comparing the results of a selector function.</summary>
        /// <param name="selector">Function selecting the actual value to be compared.</param>
        public CustomComparer<T> ThenBy<TBy>(Func<T, TBy> selector) where TBy : IComparable<TBy>
        {
            return new CustomComparer<T>((a, b) =>
            {
                int result = Compare(a, b);
                if (result != 0)
                    return result;
                else
                    return selector(a).CompareTo(selector(b));
            });
        }

        /// <summary>Creates and returns a CustomComparer which uses the current comparer first, and if the current comparer says the
        /// items are equal, further compares items by comparing the results of a selector function.</summary>
        /// <param name="selector">Function selecting the actual value to be compared.</param>
        /// <param name="selectedComparer">Comparer to use for comparing the results of the selector function.</param>
        public CustomComparer<T> ThenBy<TBy>(Func<T, TBy> selector, IComparer<TBy> selectedComparer)
        {
            return new CustomComparer<T>((a, b) =>
            {
                int result = Compare(a, b);
                if (result != 0)
                    return result;
                else
                    return selectedComparer.Compare(selector(a), selector(b));
            });
        }

        /// <summary>Creates and returns a CustomComparer which uses the current comparer first, and if the current comparer says the
        /// items are equal, further compares items by comparing the results of a selector function.</summary>
        /// <param name="selector">Function selecting the actual value to be compared.</param>
        /// <param name="selectedComparison">Comparison to use for comparing the results of the selector function.</param>
        public CustomComparer<T> ThenBy<TBy>(Func<T, TBy> selector, Comparison<TBy> selectedComparison)
        {
            return new CustomComparer<T>((a, b) =>
            {
                int result = Compare(a, b);
                if (result != 0)
                    return result;
                else
                    return selectedComparison(selector(a), selector(b));
            });
        }

        /// <summary>Creates and returns a CustomComparer which uses the current comparer first, and if the current comparer says the
        /// items are equal, further compares items by comparing the results of a string selector function, ignoring case.</summary>
        /// <param name="selector">Function selecting the actual value to be compared.</param>
        public CustomComparer<T> ThenByStringNoCase(Func<T, string> selector)
        {
            return new CustomComparer<T>((a, b) =>
            {
                int result = Compare(a, b);
                if (result != 0)
                    return result;
                else
                    return StringComparer.OrdinalIgnoreCase.Compare(selector(a), selector(b));
            });
        }
    }

    /// <summary>Encapsulates an IEqualityComparer&lt;T&gt; that uses an equality comparison function provided as a delegate.</summary>
    /// <typeparam name="T">The type of elements to be compared for equality.</typeparam>
    public sealed class CustomEqualityComparer<T> : IEqualityComparer<T>
    {
        private Func<T, T, bool> _comparison;
        private Func<T, int> _getHashCode;

        /// <summary>Constructor.</summary>
        /// <param name="comparison">Provides the comparison function for this equality comparer.</param>
        /// <param name="getHashCode">Provides the hash function for this equality comparer.</param>
        public CustomEqualityComparer(Func<T, T, bool> comparison, Func<T, int> getHashCode) { _comparison = comparison; _getHashCode = getHashCode; }
        /// <summary>Compares two elements for equality.</summary>
        /// <remarks>This method implements <see cref="IEqualityComparer&lt;T&gt;.Equals(T,T)"/>.</remarks>
        public bool Equals(T x, T y) { return _comparison(x, y); }
        /// <summary>Returns a hash code for an element.</summary>
        /// <remarks>This method implements <see cref="IEqualityComparer&lt;T&gt;.GetHashCode(T)"/>.</remarks>
        public int GetHashCode(T obj) { return _getHashCode(obj); }
    }
}
