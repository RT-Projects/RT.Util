using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RT.Util.ExtensionMethods
{
    /// <summary>
    /// Provides extension methods on the <see cref="Dictionary&lt;TKey,TValue&gt;"/> type.
    /// </summary>
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Adds an element to a List&lt;V&gt; stored in the current Dictionary&lt;K, List&lt;V&gt;&gt;.
        /// If the specified key does not exist in the current Dictionary, a new List is created.
        /// </summary>
        /// <typeparam name="K">Type of the key of the Dictionary.</typeparam>
        /// <typeparam name="V">Type of the values in the Lists.</typeparam>
        /// <param name="Dic">Dictionary to operate on.</param>
        /// <param name="Key">Key at which the list is located in the Dictionary.</param>
        /// <param name="Value">Value to add to the List located at the specified Key.</param>
        public static void AddSafe<K, V>(this Dictionary<K, List<V>> Dic, K Key, V Value)
        {
            if (!Dic.ContainsKey(Key))
                Dic[Key] = new List<V>();
            Dic[Key].Add(Value);
        }
    }
}
