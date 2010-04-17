using System;
using System.Collections.Generic;

namespace RT.KitchenSink.Collections
{
    /// <summary>
    /// Exposes an IEnumerable&lt;T&gt; as an ICollection&lt;T&gt;, ensuring that the enumerable
    /// is enumerated in a lazy fashion, only as required, and at most once. The wrapper is read-only,
    /// and all write-related methods throw a <see cref="NotSupportedException"/>.
    /// </summary>
    /// <typeparam name="T">Type of the elements in the enumeration.</typeparam>
    public class LazyToCollection<T> : ICollection<T>, IDisposable
    {
        private IEnumerator<T> _enumerator;
        private List<T> _list;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="enumerable">The enumerable to be lazily converted to a collection.</param>
        public LazyToCollection(IEnumerable<T> enumerable)
        {
            if (enumerable == null)
                throw new ArgumentNullException("enumerable");
            _enumerator = enumerable.GetEnumerator();
        }

        /// <summary>
        /// Disposes of the enumerable's enumerator, if one is held at the moment.
        /// </summary>
        public void Dispose()
        {
            if (_enumerator != null)
                _enumerator.Dispose();
        }

        /// <summary>
        /// Checks whether the specified index is a valid index, i.e. not out-of-range.
        /// </summary>
        public bool IndexInRange(int index)
        {
            if (index < 0)
                return false;
            ensureEnumeratedUpToIndex(index);
            return index < _list.Count;
        }

        private void ensureEnumeratedUpToIndex(int index)
        {
            if (_list == null)
                _list = new List<T>();
            if (_enumerator != null)
                while (_list.Count <= index)
                {
                    if (!_enumerator.MoveNext())
                    {
                        _enumerator.Dispose();
                        _enumerator = null;
                        break;
                    }
                    _list.Add(_enumerator.Current);
                }
        }

        private void ensureEnumeratedCompletely()
        {
            if (_list == null)
                _list = new List<T>();
            if (_enumerator != null)
                while (true)
                {
                    if (!_enumerator.MoveNext())
                    {
                        _enumerator.Dispose();
                        _enumerator = null;
                        break;
                    }
                    _list.Add(_enumerator.Current);
                }
        }

        /// <summary>
        /// Gets the item at the specified index.
        /// </summary>
        public T this[int index]
        {
            get
            {
                ensureEnumeratedUpToIndex(index);
                return _list[index];
            }
        }

        /// <summary>
        /// Gets the number of elements. Note that this method will have to enumerate the underlying
        /// enumerable in full.
        /// </summary>
        public int Count
        {
            get
            {
                ensureEnumeratedCompletely();
                return _list.Count;
            }
        }

        /// <summary>
        /// Returns true.
        /// </summary>
        public bool IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// Not yet implemented.
        /// </summary>
        public bool Contains(T item)
        {
            throw new NotImplementedException(); // can be implemented more efficiently than ensureEnumeratedCompletely, if required
        }

        /// <summary>
        /// Not yet implemented.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Copies all elements to the specified array starting at the specified index.
        /// </summary>
        public void CopyTo(T[] array, int arrayIndex)
        {
            ensureEnumeratedCompletely();
            _list.CopyTo(array, arrayIndex);
        }

        /// <summary>Not supported and always throws <see cref="NotSupportedException"/>.</summary>
        public void Add(T item)
        {
            throw new NotSupportedException("The LazyToCollection<T> wrapper is read-only.");
        }

        /// <param name="item"></param>
        public void Clear()
        {
            throw new NotSupportedException("The LazyToCollection<T> wrapper is read-only.");
        }

        /// <param name="item"></param>
        public bool Remove(T item)
        {
            throw new NotSupportedException("The LazyToCollection<T> wrapper is read-only.");
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
