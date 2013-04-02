using System;
using System.Collections.Generic;

namespace RT.Util.Collections
{
    /// <summary>
    ///     Implements a dictionary with a slightly different indexer, namely one that pretends every key has been pre-initialized
    ///     using an initializer function. See Remarks.</summary>
    /// <remarks>
    ///     Only the indexer behaviour is changed; in every other way this behaves just like a standard, non-prepopulated
    ///     dictionary. Moreover, the implementation is such that the new behaviour is only effective when used directly through
    ///     the class; accessing the indexer through the <c>IDictionary</c> interface or the <c>Dictionary</c> base class will
    ///     currently behave the same as it would for a standard dictionary.</remarks>
    public class AutoDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        private Func<TKey, TValue> _initializer;

        /// <summary>
        ///     Gets or sets the element with the specified key. When getting a key that hasn't been set before, the getter
        ///     behaves as if the dictionary has been pre-populated for every possible key using the initializer function (or
        ///     default(TValue) if not provided).</summary>
        public new TValue this[TKey key]
        {
            get
            {
                TValue v;
                if (TryGetValue(key, out v))
                    return v;
                v = _initializer == null ? default(TValue) : _initializer(key);
                Add(key, v);
                return v;
            }

            set
            {
                base[key] = value;
            }
        }

        /// <summary>Constructor.</summary>
        public AutoDictionary(Func<TKey, TValue> initializer = null)
            : base()
        {
            _initializer = initializer;
        }
        /// <summary>Constructor.</summary>
        public AutoDictionary(int capacity, Func<TKey, TValue> initializer = null)
            : base(capacity)
        {
            _initializer = initializer;
        }
        /// <summary>Constructor.</summary>
        public AutoDictionary(IEqualityComparer<TKey> comparer, Func<TKey, TValue> initializer = null)
            : base(comparer)
        {
            _initializer = initializer;
        }
        /// <summary>Constructor.</summary>
        public AutoDictionary(IDictionary<TKey, TValue> dictionary, Func<TKey, TValue> initializer = null)
            : base(dictionary)
        {
            _initializer = initializer;
        }
        /// <summary>Constructor.</summary>
        public AutoDictionary(int capacity, IEqualityComparer<TKey> comparer, Func<TKey, TValue> initializer = null)
            : base(capacity, comparer)
        {
            _initializer = initializer;
        }
        /// <summary>Constructor.</summary>
        public AutoDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer, Func<TKey, TValue> initializer = null)
            : base(dictionary, comparer)
        {
            _initializer = initializer;
        }
    }
}
