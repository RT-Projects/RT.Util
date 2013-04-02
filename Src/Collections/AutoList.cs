using System;
using System.Collections.Generic;

namespace RT.Util.Collections
{
    /// <summary>
    ///     Encapsulates a list which dynamically grows as items are written to non-existent indexes. Any gaps are populated with
    ///     default values. The behaviour of this list's indexed getter and setter is indistinguishable from that of an infinitely
    ///     long list pre-populated by invoking the initializer function (assuming it is side-effect free). See Remarks.</summary>
    /// <remarks>
    ///     <para>
    ///         Only the indexer behaviour is changed; in every other way this behaves just like a standard, non-infinite list.
    ///         Moreover, the implementation is such that the new behaviour is only effective when used directly through the
    ///         class; accessing the indexer through the <c>IList</c> interface or the <c>List</c> base class will currently
    ///         behave the same as it would for a standard list.</para>
    ///     <para>
    ///         Note that this is not a sparse list; accessing elements at a given index will grow the list to contain all of the
    ///         items below the index too.</para></remarks>
    public class AutoList<T> : List<T>
    {
        private Func<int, T> _initializer;

        /// <summary>
        ///     Gets or sets the element at the specified index. The behaviour of both the getter and the setter is
        ///     indistinguishable from that of an infinitely long list pre-populated by invoking the initializer function
        ///     (assuming it is side-effect free).</summary>
        public new T this[int index]
        {
            get
            {
                if (_initializer == null)
                    return index >= Count ? default(T) : base[index];
                // default(T) cannot possibly create a value that we'd need to store immediately in order to preserve the infinite list illusion,
                // so do not grow the list for this if the user supplied no initializer.

                while (index >= Count)
                    Add(_initializer(Count));
                return base[index];
            }

            set
            {
                while (index >= Count)
                    Add(_initializer == null ? default(T) : _initializer(Count));
                base[index] = value;
            }
        }

        /// <summary>
        ///     Constructor.</summary>
        /// <param name="initializer">
        ///     A function which creates a value to be used for non-existent elements upon their creation. If <c>null</c>,
        ///     <c>default(T)</c> is used instead.</param>
        public AutoList(Func<int, T> initializer = null)
            : base()
        {
            _initializer = initializer;
        }

        /// <summary>
        ///     Constructor.</summary>
        /// <param name="capacity">
        ///     The number of elements that the new list can initially store.</param>
        /// <param name="initializer">
        ///     A function which creates a value to be used for non-existent elements upon their creation. If <c>null</c>,
        ///     <c>default(T)</c> is used instead.</param>
        public AutoList(int capacity, Func<int, T> initializer = null)
            : base(capacity)
        {
            _initializer = initializer;
        }

        /// <summary>
        ///     Constructor.</summary>
        /// <param name="collection">
        ///     A collection whose elements are copied to the new list.</param>
        /// <param name="initializer">
        ///     A function which creates a value to be used for non-existent elements upon their creation. If <c>null</c>,
        ///     <c>default(T)</c> is used instead.</param>
        public AutoList(IEnumerable<T> collection, Func<int, T> initializer = null)
            : base(collection)
        {
            _initializer = initializer;
        }
    }
}
