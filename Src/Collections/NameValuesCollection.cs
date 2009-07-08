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
    public class NameValuesCollection<TValue> : IDictionary<string, ValuesCollection<TValue>>
    {
        private Dictionary<string, List<TValue>> _items;
        private bool _isReadOnly = false;
        private NameValuesCollection<TValue> _asReadOnly = null;

        public NameValuesCollection()
        {
            _items = new Dictionary<string, List<TValue>>();
        }

        public NameValuesCollection(int capacity)
        {
            _items = new Dictionary<string, List<TValue>>(capacity);
        }

        private NameValuesCollection(Dictionary<string, List<TValue>> items, bool isReadOnly)
        {
            _items = items;
            _isReadOnly = isReadOnly;
        }

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

        public void Add(string key, ValuesCollection<TValue> value)
        {
            if (_isReadOnly) throwNonWritable();
            if (_items.ContainsKey(key) && _items[key].Count == 0)
                _items[key].AddRange(value);
            else
                _items.Add(key, new List<TValue>(value));
        }

        public bool ContainsKey(string key)
        {
            return _items.ContainsKey(key) && _items[key].Count > 0;
        }

        public ICollection<string> Keys
        {
            get { return _items.Keys.Where(key => _items[key].Count > 0).ToArray(); }
        }

        public bool Remove(string key)
        {
            if (_isReadOnly) throwNonWritable();
            if (!_items.ContainsKey(key))
                return false;

            bool found = _items[key].Count > 0;
            _items.Remove(key);
            return found;
        }

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

        public void Add(KeyValuePair<string, ValuesCollection<TValue>> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            if (_isReadOnly) throwNonWritable();
            _items.Clear();
        }

        public bool Contains(KeyValuePair<string, ValuesCollection<TValue>> item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(KeyValuePair<string, ValuesCollection<TValue>>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, ValuesCollection<TValue>>>) _items).CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _items.Values.Count(list => list.Count > 0); }
        }

        public bool IsReadOnly
        {
            get { return _isReadOnly; }
        }

        public bool Remove(KeyValuePair<string, ValuesCollection<TValue>> item)
        {
            if (_isReadOnly) throwNonWritable();
            throw new NotImplementedException();
        }

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

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public struct ValuesCollection<TValue> : IList<TValue>
    {
        /// <summary>The collection being wrapped. Both "null" and an empty list may stand for empty.</summary>
        private List<TValue> _values;
        /// <summary>
        /// If false, the wrapped collection cannot be modified through this struct.
        /// The reason this is in the opposite sense to "isReadOnly" is that this way default(ValuesCollection)
        /// results in a valid, empty read-only collection.
        /// </summary>
        private bool _isWritable;

        /// <summary>
        /// Creates a wrapper for the specified list. The list may be null to represent an empty
        /// wrapper provided that the wrapper is read-only.
        /// </summary>
        /// <param name="values">The list to wrap. May be null for an empty read-only wrapper.</param>
        /// <param name="isReadOnly">Specifies whether the wrapper allows the wrapped list to be modified.</param>
        public ValuesCollection(List<TValue> values, bool isReadOnly)
        {
            if (values == null && !isReadOnly) throw new ArgumentException("A ValuesCollection<> with a null underlying list must be created as read-only.");
            _values = values;
            _isWritable = !isReadOnly;
        }

        private void throwNonWritable()
        {
            throw new InvalidOperationException("Cannot execute method because the collection is read-only");
        }

        public int IndexOf(TValue item)
        {
            return _values == null ? -1 : _values.IndexOf(item);
        }

        public void Insert(int index, TValue item)
        {
            if (!_isWritable) throwNonWritable();
            _values.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            if (!_isWritable) throwNonWritable();
            _values.RemoveAt(index);
        }

        public TValue this[int index]
        {
            get
            {
                if (_values == null) throw new ArgumentOutOfRangeException("The ValuesCollection is empty.");
                else return _values[index];
            }
            set
            {
                if (!_isWritable) throwNonWritable();
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

        public void Add(TValue item)
        {
            if (!_isWritable) throwNonWritable();
            _values.Add(item);
        }

        public void Clear()
        {
            if (!_isWritable) throwNonWritable();
            _values.Clear();
        }

        public bool Contains(TValue item)
        {
            return _values == null ? false : _values.Contains(item);
        }

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            if (_values != null)
                _values.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _values == null ? 0 : _values.Count; }
        }

        public bool IsReadOnly
        {
            get { return !_isWritable; }
        }

        public bool Remove(TValue item)
        {
            if (!_isWritable) throwNonWritable();
            return _values.Remove(item);
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            return _values == null ? ((IEnumerable<TValue>) new TValue[0]).GetEnumerator() : _values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _values == null ? new TValue[0].GetEnumerator() : _values.GetEnumerator();
        }

        public override string ToString()
        {

            if (_values == null || _values.Count == 0)
                return "[]";
            else
                return "[" + _values.Select(val => "\"" + val.ToString() + "\"").JoinString(", ") + "]";
        }
    }
}
