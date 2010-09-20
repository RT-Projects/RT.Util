using System.Collections.Generic;

namespace RT.Util.Collections
{
    /// <summary>Encapsulates a list which dynamically grows as items are written to non-existent indexes. The indexes are filled with the type argument’s default value.</summary>
    public class AutoList<T> : List<T>
    {
        /// <summary>Gets or sets the element at the specified index.</summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index, or the type argument’s default value if the index does not exist.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">index is less than 0.</exception>
        public new T this[int index]
        {
            get
            {
                return index >= Count ? default(T) : base[index];
            }
            set
            {
                while (index >= Count)
                    Add(default(T));
                base[index] = value;
            }
        }

        /// <summary>Constructor.</summary>
        public AutoList() : base() { }
        /// <summary>Constructor.</summary>
        /// <param name="capacity">The number of elements that the new list can initially store.</param>
        public AutoList(int capacity) : base(capacity) { }
        /// <summary>Constructor.</summary>
        /// <param name="collection">A collection whose elements are copied to the new list.</param>
        public AutoList(IEnumerable<T> collection) : base(collection) { }
    }
}
