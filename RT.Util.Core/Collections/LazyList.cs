namespace RT.KitchenSink.Collections;

/// <summary>
///     Exposes an IEnumerable&lt;T&gt; as an IList&lt;T&gt;, enabling buffering of and indexing into the elements generated
///     by the enumerable, while still ensuring that the enumerable is enumerated in a lazy fashion, only as required, and at
///     most once. This collection is read-only, and all write-related methods throw a <see cref="NotSupportedException"/>.</summary>
/// <typeparam name="T">
///     Type of the elements in the enumeration.</typeparam>
public class LazyList<T> : IList<T>, IDisposable
{
    private IEnumerator<T> _enumerator;
    private List<T> _list;

    /// <summary>
    ///     Constructor.</summary>
    /// <param name="enumerable">
    ///     The enumerable to be lazily converted to a list.</param>
    public LazyList(IEnumerable<T> enumerable)
    {
        if (enumerable == null)
            throw new ArgumentNullException(nameof(enumerable));
        _enumerator = enumerable.GetEnumerator();
    }

    /// <summary>Disposes of the enumerable's enumerator, if one is held at the moment.</summary>
    public void Dispose()
    {
        if (_enumerator != null)
            _enumerator.Dispose();
    }

    /// <summary>Checks whether the specified index is a valid index, i.e. not out-of-range.</summary>
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

    /// <summary>Gets the item at the specified index.</summary>
    public T this[int index]
    {
        get
        {
            ensureEnumeratedUpToIndex(index);
            return _list[index];
        }
    }

    /// <summary>Gets the number of elements. Note that this method will have to enumerate the underlying enumerable in full.</summary>
    public int Count
    {
        get
        {
            ensureEnumeratedCompletely();
            return _list.Count;
        }
    }

    /// <summary>Returns true.</summary>
    public bool IsReadOnly
    {
        get { return true; }
    }

    /// <summary>
    ///     Determines whether an element is in the list.</summary>
    /// <param name="item">
    ///     The object to locate in the list. The value can be null for reference types.</param>
    public bool Contains(T item)
    {
        return IndexOf(item) != -1;
    }

    /// <summary>
    ///     Returns an enumerator for the collection. Warning: This enumerates the underlying collection completely before
    ///     returning.</summary>
    public IEnumerator<T> GetEnumerator()
    {
        ensureEnumeratedCompletely();
        return _list.GetEnumerator();
    }

    /// <summary>Copies all elements to the specified array starting at the specified index.</summary>
    public void CopyTo(T[] array, int arrayIndex)
    {
        ensureEnumeratedCompletely();
        _list.CopyTo(array, arrayIndex);
    }

    void ICollection<T>.Add(T item)
    {
        throw new NotSupportedException("The LazyList<T> wrapper is read-only.");
    }

    void ICollection<T>.Clear()
    {
        throw new NotSupportedException("The LazyList<T> wrapper is read-only.");
    }

    bool ICollection<T>.Remove(T item)
    {
        throw new NotSupportedException("The LazyList<T> wrapper is read-only.");
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    ///     Returns the index of the specified item, or -1 if the item is not found.</summary>
    /// <param name="item">
    ///     The item to find in the collection.</param>
    /// <remarks>
    ///     The underlying collection is enumerated until the item is found, or fully if the item is not in it.</remarks>
    public int IndexOf(T item)
    {
        if (_list == null)
            _list = new List<T>();
        var pos = _list.IndexOf(item);
        if (pos != -1)
            return pos;
        if (_enumerator == null)
            return -1;
        while (true)
        {
            if (!_enumerator.MoveNext())
            {
                _enumerator.Dispose();
                _enumerator = null;
                return -1;
            }
            _list.Add(_enumerator.Current);
            if (_enumerator.Current.Equals(item))
                return _list.Count - 1;
        }
    }

    void IList<T>.Insert(int index, T item)
    {
        throw new NotSupportedException("The LazyList<T> wrapper is read-only.");
    }

    void IList<T>.RemoveAt(int index)
    {
        throw new NotSupportedException("The LazyList<T> wrapper is read-only.");
    }

    T IList<T>.this[int index]
    {
        get { return this[index]; }
        set { throw new NotSupportedException("The LazyList<T> wrapper is read-only."); }
    }

    bool ICollection<T>.IsReadOnly { get { return true; } }
}
