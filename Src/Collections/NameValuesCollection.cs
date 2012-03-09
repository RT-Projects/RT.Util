using System;
using System.Collections.Generic;
using System.Linq;
using RT.Util.ExtensionMethods;

namespace RT.Util.Collections
{
    /// <summary>
    /// Encapsulates a collection that maps keys to collections of values.
    /// Provides the ability to make the collection fully read-only, cheaply.
    /// Provides a sort of auto-vivification for convenience. Example:
    /// <code>
    ///     // initially myNameValue does not contain the key "fruits"
    ///     int c = myNameValues["fruits"].Count;  // c == 0
    ///     myNameValues["fruits"].Add("orange");
    ///     // myNameValue now contains the key "fruits", with one value associated.
    /// </code>
    /// </summary>
    /// <typeparam name="TValue">The type of the values to be associated with each key.</typeparam>
    public sealed class NameValuesCollection<TValue> : IDictionary<string, ValuesCollection<TValue>>
    {
        private Dictionary<string, List<TValue>> _items;
        private bool _isReadOnly = false;
        private NameValuesCollection<TValue> _asReadOnly = null;

        /// <summary>
        /// Creates an empty collection.
        /// </summary>
        public NameValuesCollection()
        {
            _items = new Dictionary<string, List<TValue>>();
        }

        /// <summary>
        /// Creates an empty collection.
        /// </summary>
        /// <param name="capacity">Initial capacity, in terms of keys.</param>
        public NameValuesCollection(int capacity)
        {
            _items = new Dictionary<string, List<TValue>>(capacity);
        }

        private NameValuesCollection(Dictionary<string, List<TValue>> items, bool isReadOnly)
        {
            _items = items;
            _isReadOnly = isReadOnly;
        }

        /// <summary>
        /// Returns a read-only version of this collection. The returned collection could be the
        /// same as this one, if it's already read-only, or it could be a wrapper created around
        /// the items in this collection. If the original collection gets modified, the read-only
        /// version will reflect the changes instantly.
        /// </summary>
        public NameValuesCollection<TValue> AsReadOnly()
        {
            if (_isReadOnly)
                return this;
            if (_asReadOnly == null)
                _asReadOnly = new NameValuesCollection<TValue>(_items, true);
            return _asReadOnly;
        }

        private void throwNonWritable()
        {
            throw new InvalidOperationException("Cannot execute method because the collection is read-only");
        }

        /// <summary>
        /// Adds all items from the specified value collection, associating them with
        /// the specified key. Throws an exception if the specified key already has items
        /// associated with it.
        /// </summary>
        /// <param name="key">Key to associate the items with.</param>
        /// <param name="value">Collection containing the values to add.</param>
        public void Add(string key, ValuesCollection<TValue> value)
        {
            if (_isReadOnly) throwNonWritable();
            if (_items.ContainsKey(key) && _items[key].Count == 0)
                _items[key].AddRange(value);
            else
                _items.Add(key, new List<TValue>(value));
        }

        /// <summary>
        /// Returns true iff this collection has at least one value associated with the specified key.
        /// </summary>
        public bool ContainsKey(string key)
        {
            return _items.ContainsKey(key) && _items[key].Count > 0;
        }

        /// <summary>
        /// Gets a collection of the keys that have at least one value associated with them. This
        /// method is not particularly cheap.
        /// </summary>
        public ICollection<string> Keys
        {
            get { return _items.Keys.Where(key => _items[key].Count > 0).ToArray(); }
        }

        /// <summary>
        /// Removes all items associated with the specified key. Returns true iff any items were removed.
        /// </summary>
        public bool Remove(string key)
        {
            if (_isReadOnly) throwNonWritable();
            if (!_items.ContainsKey(key))
                return false;

            bool found = _items[key].Count > 0;
            _items.Remove(key);
            return found;
        }

        /// <summary>
        /// Gets the collection of values associated with the specified key, or an empty collection
        /// if no such items exist. Returns true iff any items are associated with the key.
        /// </summary>
        public bool TryGetValue(string key, out ValuesCollection<TValue> value)
        {
            if (_items.ContainsKey(key) && _items[key].Count > 0)
            {
                value = new ValuesCollection<TValue>(_items[key], _isReadOnly);
                return true;
            }
            else
            {
                value = default(ValuesCollection<TValue>);
                return false;
            }
        }

        /// <summary>
        /// Gets the collection of all value collections. This method is not particularly cheap.
        /// </summary>
        public ICollection<ValuesCollection<TValue>> Values
        {
            get
            {
                return _items.Values
                    .Where(list => list.Count > 0)
                    .Select(list => new ValuesCollection<TValue>(list, _isReadOnly))
                    .ToArray();
            }
        }

        /// <summary>
        /// Gets or sets a collection of values associated with the specified key.
        /// </summary>
        public ValuesCollection<TValue> this[string key]
        {
            get
            {
                if (_items.ContainsKey(key))
                    return new ValuesCollection<TValue>(_items[key], _isReadOnly);
                else
                {
                    if (_isReadOnly)
                    {
                        // skip vivification because the list can't be modified
                        return new ValuesCollection<TValue>(null, true);
                    }
                    else
                    {
                        // must perform vivification
                        var list = new List<TValue>();
                        _items.Add(key, list);
                        return new ValuesCollection<TValue>(list, false);
                    }
                }
            }
            set
            {
                if (_isReadOnly) throwNonWritable();
                if (_items.ContainsKey(key) && _items[key].Count == 0)
                    _items[key].AddRange(value);
                else
                    _items[key] = new List<TValue>(value);
            }
        }

        /// <summary>
        /// Adds the specified key and value collection.
        /// </summary>
        public void Add(KeyValuePair<string, ValuesCollection<TValue>> item)
        {
            Add(item.Key, item.Value);
        }

        /// <summary>
        /// Clears all items from this collection.
        /// </summary>
        public void Clear()
        {
            if (_isReadOnly) throwNonWritable();
            _items.Clear();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public bool Contains(KeyValuePair<string, ValuesCollection<TValue>> item)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Copies all key/value-collection pairs to the specified array.
        /// </summary>
        public void CopyTo(KeyValuePair<string, ValuesCollection<TValue>>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, ValuesCollection<TValue>>>) _items).CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Gets the number of keys in this collection which have at least one value associated with them.
        /// </summary>
        public int Count
        {
            get { return _items.Values.Count(list => list.Count > 0); }
        }

        /// <summary>
        /// Returns true iff this collection is read-only (cannot be modified).
        /// </summary>
        public bool IsReadOnly
        {
            get { return _isReadOnly; }
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public bool Remove(KeyValuePair<string, ValuesCollection<TValue>> item)
        {
            if (_isReadOnly) throwNonWritable();
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets an enumerator to iterate over all key/value-collection pairs in this collection.
        /// </summary>
        public IEnumerator<KeyValuePair<string, ValuesCollection<TValue>>> GetEnumerator()
        {
            foreach (var kvp in _items)
            {
                if (kvp.Value.Count > 0)
                {
                    yield return new KeyValuePair<string, ValuesCollection<TValue>>(
                        kvp.Key,
                        new ValuesCollection<TValue>(kvp.Value, _isReadOnly));
                }
            }
        }

        /// <summary>
        /// Gets an enumerator to iterate over all key/value-collection pairs in this collection.
        /// </summary>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// Represents a collection of values associated with a single key inside a <see cref="NameValuesCollection&lt;T&gt;"/>.
    /// </summary>
    /// <remarks>
    /// Implemented as a wrapper for a List which can be read-only or read-write as desired. Creation should
    /// be cheap compared to instantiating a <see cref="ReadOnlyCollection&lt;T&gt;"/>.
    /// </remarks>
    /// <typeparam name="TValue">The type of the values stored in this collection.</typeparam>
    public struct ValuesCollection<TValue> : IList<TValue>
    {
        /// <summary>The collection being wrapped. Both null and an empty list may stand for empty.</summary>
        private List<TValue> _values;

        /// <summary>
        /// If false, the wrapped collection cannot be modified through this struct (read-only).
        /// The reason this isn’t “isReadOnly” is that <c>default(ValuesCollection&lt;TValue&gt;)</c>
        /// and <c>new ValuesCollection&lt;TValue&gt;()</c> would otherwise generate an invalid
        /// collection: <paramref name="_values"/> would be null, so no values could be added.
        /// </summary>
        private bool _isWritable;

        /// <summary>
        /// Creates a wrapper for the specified list instance. The list may be null to represent an empty
        /// wrapper provided that the wrapper is read-only.
        /// </summary>
        /// <param name="values">The list to wrap. May be null for an empty read-only wrapper.</param>
        /// <param name="isReadOnly">Specifies whether the wrapper allows the wrapped list to be modified.</param>
        /// <remarks>This constructor does NOT create a copy of the list. Thus, even if <paramref name="isReadOnly"/> is true,
        /// the list may still be modified if a reference to the list is accessed elsewhere. Consider only passing in lists which you
        /// created and are not passed or used anywhere else.</remarks>
        public ValuesCollection(List<TValue> values, bool isReadOnly)
        {
            if (values == null && !isReadOnly)
                throw new ArgumentException("A {0} with a null underlying list must be created as read-only.".Fmt(typeof(ValuesCollection<TValue>).FullName));
            _values = values;
            _isWritable = !isReadOnly;
        }

        /// <summary>Creates a writable (non-read-only) empty <see cref="ValuesCollection{TValue}"/>.</summary>
        public static ValuesCollection<TValue> CreateEmpty() { return new ValuesCollection<TValue>(new List<TValue>(), isReadOnly: false); }

        /// <summary>Creates a writable (non-read-only) <see cref="ValuesCollection{TValue}"/> containing the specified values.</summary>
        public static ValuesCollection<TValue> Create(params TValue[] initialValues) { return new ValuesCollection<TValue>(new List<TValue>(initialValues), isReadOnly: false); }

        /// <summary>Creates a read-only <see cref="ValuesCollection{TValue}"/> containing the specified values.</summary>
        public static ValuesCollection<TValue> CreateReadOnly(params TValue[] initialValues) { return new ValuesCollection<TValue>(new List<TValue>(initialValues), isReadOnly: true); }

        /// <summary>Creates a read-only <see cref="ValuesCollection{TValue}"/> containing the specified values.</summary>
        public static implicit operator ValuesCollection<TValue>(TValue[] initialValues) { return CreateReadOnly(initialValues); }

        /// <summary>Creates a read-only <see cref="ValuesCollection{TValue}"/> wrapping the specified list instance.</summary>
        /// <remarks>This operator does NOT create a copy of the list. Thus, even if <paramref name="isReadOnly"/> is true,
        /// the list may still be modified if a reference to the list is accessed elsewhere. Consider only passing in lists which you
        /// created and are not passed or used anywhere else.</remarks>
        public static implicit operator ValuesCollection<TValue>(List<TValue> values) { return new ValuesCollection<TValue>(values, isReadOnly: true); }

        private void throwIfReadOnly()
        {
            if (!_isWritable)
                throw new InvalidOperationException("Cannot execute method because the collection is read-only.");
        }

        /// <summary>
        /// Returns the index of the specified item in this collection, or -1 if not found.
        /// </summary>
        /// <param name="item">Item to be searched for.</param>
        public int IndexOf(TValue item)
        {
            return _values == null ? -1 : _values.IndexOf(item);
        }

        /// <summary>
        /// Inserts the item at the specified position.
        /// </summary>
        public void Insert(int index, TValue item)
        {
            throwIfReadOnly();
            _values.Insert(index, item);
        }

        /// <summary>
        /// Removes the item at the specified position from the collection.
        /// </summary>
        public void RemoveAt(int index)
        {
            throwIfReadOnly();
            _values.RemoveAt(index);
        }

        /// <summary>
        /// Gets or sets the value at the specified index.
        /// </summary>
        public TValue this[int index]
        {
            get
            {
                if (_values == null) throw new ArgumentOutOfRangeException("The ValuesCollection is empty.");
                else return _values[index];
            }
            set
            {
                throwIfReadOnly();
                _values[index] = value;
            }
        }

        /// <summary>
        /// Gets the first value stored in this value collection. If the collection is empty,
        /// returns the default value for <typeparamref name="TValue"/> (i.e. null for all reference types).
        /// </summary>
        public TValue Value
        {
            get
            {
                return _values == null || _values.Count == 0 ? default(TValue) : _values[0];
            }
        }

        /// <summary>
        /// Adds the specified value to the collection.
        /// </summary>
        public void Add(TValue item)
        {
            throwIfReadOnly();
            _values.Add(item);
        }

        /// <summary>
        /// Clears all items from the collection.
        /// </summary>
        public void Clear()
        {
            throwIfReadOnly();
            _values.Clear();
        }

        /// <summary>
        /// Returns true iff the collection contains the specified item.
        /// </summary>
        public bool Contains(TValue item)
        {
            return _values == null ? false : _values.Contains(item);
        }

        /// <summary>
        /// Copies all items in this collection to the specified array.
        /// </summary>
        public void CopyTo(TValue[] array, int arrayIndex)
        {
            if (_values != null)
                _values.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Gets the count of items stored in this collection.
        /// </summary>
        public int Count
        {
            get { return _values == null ? 0 : _values.Count; }
        }

        /// <summary>
        /// Returns true iff this collection is read-only (cannot be modified).
        /// </summary>
        public bool IsReadOnly
        {
            get { return !_isWritable; }
        }

        /// <summary>
        /// Removes the specified item from the collection. Returns true iff an item was removed.
        /// </summary>
        public bool Remove(TValue item)
        {
            throwIfReadOnly();
            return _values.Remove(item);
        }

        /// <summary>
        /// Returns an enumerator to iterate over all items in this collection.
        /// </summary>
        public IEnumerator<TValue> GetEnumerator()
        {
            return _values == null ? ((IEnumerable<TValue>) new TValue[0]).GetEnumerator() : _values.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator to iterate over all items in this collection.
        /// </summary>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _values == null ? new TValue[0].GetEnumerator() : _values.GetEnumerator();
        }

        /// <summary>
        /// Returns a string listing all values in the collection, comma-separated, inside square brackets.
        /// </summary>
        public override string ToString()
        {

            if (_values == null || _values.Count == 0)
                return "[]";
            else
                return "[" + _values.Select(val => "\"" + val.ToString() + "\"").JoinString(", ") + "]";
        }
    }
}
