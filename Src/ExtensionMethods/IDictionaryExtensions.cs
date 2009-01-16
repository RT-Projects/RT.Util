using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RT.Util.ExtensionMethods
{
    /// <summary>
    /// Contains extension methods for IDictionary classes.
    /// </summary>
    public static class IDictionaryExtensions
    {
        /// <summary>
        /// Compares two dictionaries for equality, member-wise. Two dictionaries are equal if
        /// they contain all the same key-value pairs.
        /// </summary>
        public static bool DictionaryEqual<TK, TV>(this IDictionary<TK, TV> dictA, IDictionary<TK, TV> dictB)
            where TV: IEquatable<TV>
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
