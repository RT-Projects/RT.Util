using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
