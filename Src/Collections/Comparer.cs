using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RT.Util.Collections
{
    /// <summary>Encapsulates an IComparer&lt;T&gt; that uses a comparison function provided as a delegate.</summary>
    /// <typeparam name="T">The type of elements to be compared.</typeparam>
    public class CustomComparer<T> : IComparer<T>
    {
        private Comparison<T> _comparison;
        /// <summary>Constructor.</summary>
        /// <param name="comparison">Provides the comparison function for this comparer.</param>
        public CustomComparer(Comparison<T> comparison) { _comparison = comparison; }
        /// <summary>Compares two elements.</summary>
        /// <remarks>This method implements <see cref="IComparer&lt;T&gt;.Compare(T,T)"/>.</remarks>
        public int Compare(T x, T y) { return _comparison(x, y); }
    }

    /// <summary>Provides a static method to create a <see cref="CustomComparer&lt;T&gt;"/> from a delegate.</summary>
    public static class CustomComparer
    {
        /// <summary>Creates an instance of <see cref="CustomComparer&lt;T&gt;"/> using the specified delegate.</summary>
        /// <typeparam name="T">The type of elements to be compared.</typeparam>
        /// <param name="comparison">Provides the comparison function for this comparer.</param>
        public static CustomComparer<T> Create<T>(Comparison<T> comparison)
        {
            return new CustomComparer<T>(comparison);
        }
    }
}
