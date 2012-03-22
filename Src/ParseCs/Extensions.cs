using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RT.ParseCs
{
    public static class Extensions
    {
        public static string Indent(this string input)
        {
            return Regex.Replace(input, "^(?!$)", "    ", RegexOptions.Multiline);
        }
        public static string CsEscape(this char ch, bool singleQuote, bool doubleQuote)
        {
            switch (ch)
            {
                case '\\': return "\\\\";
                case '\0': return "\\0";
                case '\a': return "\\a";
                case '\b': return "\\b";
                case '\f': return "\\f";
                case '\n': return "\\n";
                case '\r': return "\\r";
                case '\t': return "\\t";
                case '\v': return "\\v";
                case '\'': return singleQuote ? "\\'" : "'";
                case '"': return doubleQuote ? "\\\"" : "\"";
                default: return ch.ToString();
            }
        }
        public static string ToCs(this BinaryOperator op)
        {
            return
                op == BinaryOperator.Times ? "*" :
                op == BinaryOperator.Div ? "/" :
                op == BinaryOperator.Mod ? "%" :
                op == BinaryOperator.Plus ? "+" :
                op == BinaryOperator.Minus ? "-" :
                op == BinaryOperator.Shl ? "<<" :
                op == BinaryOperator.Shr ? ">>" :
                op == BinaryOperator.Less ? "<" :
                op == BinaryOperator.Greater ? ">" :
                op == BinaryOperator.LessEq ? "<=" :
                op == BinaryOperator.GreaterEq ? ">=" :
                op == BinaryOperator.Eq ? "==" :
                op == BinaryOperator.NotEq ? "!=" :
                op == BinaryOperator.And ? "&" :
                op == BinaryOperator.Xor ? "^" :
                op == BinaryOperator.Or ? "|" :
                op == BinaryOperator.AndAnd ? "&&" :
                op == BinaryOperator.OrOr ? "||" :
                op == BinaryOperator.Coalesce ? "??" : null;
        }
        public static string ToCs(this UnaryOperator op)
        {
            return
                op == UnaryOperator.Plus ? "+" :
                op == UnaryOperator.Minus ? "-" :
                op == UnaryOperator.Not ? "!" :
                op == UnaryOperator.Neg ? "~" :
                op == UnaryOperator.PrefixInc ? "++" :
                op == UnaryOperator.PrefixDec ? "--" :
                op == UnaryOperator.PostfixInc ? "++" :
                op == UnaryOperator.PostfixDec ? "--" :
                op == UnaryOperator.PointerDeref ? "*" :
                op == UnaryOperator.AddressOf ? "&" :
                op == UnaryOperator.True ? "true" :
                op == UnaryOperator.False ? "false" : null;
        }
        public static string Sanitize(this string identifier)
        {
            if (Lexer.Keywords.Contains(identifier))
                return "@" + identifier;
            return identifier;
        }

        public static string Fmt(this string format, object arg0)
        {
            return string.Format(format, arg0);
        }
        public static string Fmt(this string format, object arg0, object arg1)
        {
            return string.Format(format, arg0, arg1);
        }
        public static string Fmt(this string format, object arg0, object arg1, object arg2)
        {
            return string.Format(format, arg0, arg1, arg2);
        }
        public static string Fmt(this string format, params object[] args)
        {
            return string.Format(format, args);
        }

        /// <summary>
        /// Returns the index of the first element in this <paramref name="source"/> satisfying
        /// the specified <paramref name="predicate"/>. If no such elements are found, returns -1.
        /// </summary>
        public static int IndexOf<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (predicate == null)
                throw new ArgumentNullException("predicate");
            int index = 0;
            foreach (var v in source)
            {
                if (predicate(v))
                    return index;
                index++;
            }
            return -1;
        }

        /// <summary>
        /// <para>Turns all elements in the enumerable to strings and joins them using the specified string
        /// as the separator and the specified prefix and suffix for each string.</para>
        /// <example>
        ///     <code>
        ///         var a = (new[] { "Paris", "London", "Tokyo" }).JoinString(", ", "[", "]");
        ///         // a contains "[Paris], [London], [Tokyo]"
        ///         ...JoinString(", ", "[", "]", " and ");
        ///         // a contains "[Paris], [London] and [Tokyo]"
        ///     </code>
        /// </example>
        /// </summary>
        public static string JoinString<T>(this IEnumerable<T> values, string separator = null, string prefix = null, string suffix = null, string lastSeparator = null)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            if (lastSeparator == null)
                lastSeparator = separator;

            using (var enumerator = values.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    return "";

                // Optimise the case where there is only one element
                var one = enumerator.Current;
                if (!enumerator.MoveNext())
                    return prefix + one + suffix;

                // Optimise the case where there are only two elements
                var two = enumerator.Current;
                if (!enumerator.MoveNext())
                {
                    // Optimise the (common) case where there is no prefix/suffix; this prevents an array allocation when calling string.Concat()
                    if (prefix == null && suffix == null)
                        return one + lastSeparator + two;
                    return prefix + one + suffix + lastSeparator + prefix + two + suffix;
                }

                StringBuilder sb = new StringBuilder()
                    .Append(prefix).Append(one).Append(suffix).Append(separator)
                    .Append(prefix).Append(two).Append(suffix);
                var prev = enumerator.Current;
                while (enumerator.MoveNext())
                {
                    sb.Append(separator).Append(prefix).Append(prev).Append(suffix);
                    prev = enumerator.Current;
                }
                sb.Append(lastSeparator).Append(prefix).Append(prev).Append(suffix);
                return sb.ToString();
            }
        }

        /// <summary>
        /// Adds an element to a List&lt;V&gt; stored in the current IDictionary&lt;K, List&lt;V&gt;&gt;.
        /// If the specified key does not exist in the current IDictionary, a new List is created.
        /// </summary>
        /// <typeparam name="K">Type of the key of the IDictionary.</typeparam>
        /// <typeparam name="V">Type of the values in the Lists.</typeparam>
        /// <param name="dic">IDictionary to operate on.</param>
        /// <param name="key">Key at which the list is located in the IDictionary.</param>
        /// <param name="value">Value to add to the List located at the specified Key.</param>
        public static void AddSafe<K, V>(this IDictionary<K, List<V>> dic, K key, V value)
        {
            if (dic == null)
                throw new ArgumentNullException("dic");
            if (key == null)
                throw new ArgumentNullException("key", "Null values cannot be used for keys in dictionaries.");
            if (!dic.ContainsKey(key))
                dic[key] = new List<V>();
            dic[key].Add(value);
        }

        /// <summary>
        /// Determines whether the current type is or implements the specified generic interface, and determines that interface's generic type parameters.
        /// </summary>
        /// <param name="type">The current type.</param>
        /// <param name="interface">A generic type definition for an interface, e.g. typeof(ICollection&lt;&gt;) or typeof(IDictionary&lt;,&gt;).</param>
        /// <param name="typeParameters">Will receive an array containing the generic type parameters of the interface.</param>
        /// <returns>True if the current type is or implements the specified generic interface.</returns>
        public static bool TryGetInterfaceGenericParameters(this Type type, Type @interface, out Type[] typeParameters)
        {
            typeParameters = null;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == @interface)
            {
                typeParameters = type.GetGenericArguments();
                return true;
            }

            var implements = type.FindInterfaces((ty, obj) => ty.IsGenericType && ty.GetGenericTypeDefinition() == @interface, null).FirstOrDefault();
            if (implements == null)
                return false;

            typeParameters = implements.GetGenericArguments();
            return true;
        }
    }
}
