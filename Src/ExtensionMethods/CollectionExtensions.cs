using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RT.Util.ExtensionMethods
{
    /// <summary>
    /// Provides extension methods on the <see cref="Dictionary&lt;TKey,TValue&gt;"/> type.
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Adds an element to a List&lt;V&gt; stored in the current Dictionary&lt;K, List&lt;V&gt;&gt;.
        /// If the specified key does not exist in the current Dictionary, a new List is created.
        /// </summary>
        /// <typeparam name="K">Type of the key of the Dictionary.</typeparam>
        /// <typeparam name="V">Type of the values in the Lists.</typeparam>
        /// <param name="dic">Dictionary to operate on.</param>
        /// <param name="key">Key at which the list is located in the Dictionary.</param>
        /// <param name="value">Value to add to the List located at the specified Key.</param>
        public static void AddSafe<K, V>(this Dictionary<K, List<V>> dic, K key, V value)
        {
            if (!dic.ContainsKey(key))
                dic[key] = new List<V>();
            dic[key].Add(value);
        }

        /// <summary>
        /// Brings the elements of the given list into a random order
        /// </summary>
        /// <typeparam name="T">Type of elements in the list.</typeparam>
        /// <param name="list">List to shuffle.</param>
        public static void Shuffle<T>(this IList<T> list)
        {
            for (int j = list.Count; j >= 1; j--)
            {
                int item = Ut.RndInt(0, j);
                if (item < j - 1)
                {
                    var t = list[item];
                    list[item] = list[j - 1];
                    list[j - 1] = t;
                }
            }
        }

        /// <summary>
        /// Compares two dictionaries for equality, member-wise. Two dictionaries are equal if
        /// they contain all the same key-value pairs.
        /// </summary>
        public static bool DictionaryEqual<TK, TV>(this IDictionary<TK, TV> dictA, IDictionary<TK, TV> dictB)
            where TV : IEquatable<TV>
        {
            if (dictA.Count != dictB.Count)
                return false;
            foreach (var key in dictA.Keys)
            {
                if (!dictB.ContainsKey(key))
                    return false;
                if (!dictA[key].Equals(dictB[key]))
                    return false;
            }
            return true;
        }
    }
}
