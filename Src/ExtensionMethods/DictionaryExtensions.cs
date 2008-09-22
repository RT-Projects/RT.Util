using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RT.Util.ExtensionMethods
{
    public static class DictionaryExtensions
    {
        public static void AddSafe<K, V>(this Dictionary<K, List<V>> Dic, K Key, V Value)
        {
            if (!Dic.ContainsKey(Key))
                Dic[Key] = new List<V>();
            Dic[Key].Add(Value);
        }
    }
}
