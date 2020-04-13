﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace RT.Util.ExtensionMethods
{
    /// <summary>
    ///     Provides extension methods on various collection types or interfaces in the System.Collections.Generic namespace
    ///     such as <see cref="Dictionary&lt;K,V&gt;"/> and on arrays.</summary>
#if EXPORT_UTIL
    public
#endif
    static class CollectionExtensions
    {
        /// <summary>Determines whether the current HashSet-in-a-Dictionary contains the specified key and value.</summary>
        public static bool Contains<TKey, TValue>(this IDictionary<TKey, HashSet<TValue>> source, TKey key, TValue value)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (key == null)
                throw new ArgumentNullException(nameof(key), "Null values cannot be used for keys in dictionaries.");
            return source.ContainsKey(key) && source[key].Contains(value);
        }

        /// <summary>
        ///     Determines whether the current two-level dictionary contains the specified combination of keys.</summary>
        /// <typeparam name="TKey1">
        ///     Type of the first-level key.</typeparam>
        /// <typeparam name="TKey2">
        ///     Type of the second-level key.</typeparam>
        /// <typeparam name="TValue">
        ///     Type of values in the dictionary.</typeparam>
        /// <param name="source">
        ///     Source dictionary to examine.</param>
        /// <param name="key1">
        ///     The first key to check for.</param>
        /// <param name="key2">
        ///     The second key to check for.</param>
        public static bool ContainsKeys<TKey1, TKey2, TValue>(this IDictionary<TKey1, Dictionary<TKey2, TValue>> source, TKey1 key1, TKey2 key2) =>
            source == null ? throw new ArgumentNullException(nameof(source)) : !source.TryGetValue(key1, out var dic) ? false : dic.ContainsKey(key2);

        /// <summary>
        ///     Gets the value associated with the specified combination of keys.</summary>
        /// <typeparam name="TKey1">
        ///     Type of the first-level key.</typeparam>
        /// <typeparam name="TKey2">
        ///     Type of the second-level key.</typeparam>
        /// <typeparam name="TValue">
        ///     Type of values in the dictionary.</typeparam>
        /// <param name="source">
        ///     Source dictionary to examine.</param>
        /// <param name="key1">
        ///     The first key to check for.</param>
        /// <param name="key2">
        ///     The second key to check for.</param>
        /// <param name="value">
        ///     When this method returns, the value associated with the specified keys, if the keys are found; otherwise, the
        ///     default value for the type of the <paramref name="value"/> parameter. This parameter is passed uninitialized.</param>
        /// <returns>
        ///     <c>true</c> if the two-level dictionary contains an element with the specified combination of keys; otherwise,
        ///     <c>false</c>.</returns>
        public static bool TryGetValue<TKey1, TKey2, TValue>(this IDictionary<TKey1, Dictionary<TKey2, TValue>> source, TKey1 key1, TKey2 key2, out TValue value)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            value = default(TValue);
            return source.TryGetValue(key1, out var dic) ? dic.TryGetValue(key2, out value) : false;
        }

        /// <summary>
        ///     Compares two dictionaries for equality, member-wise. Two dictionaries are equal if they contain all the same
        ///     key-value pairs.</summary>
        public static bool DictionaryEqual<TK, TV>(this IDictionary<TK, TV> dictA, IDictionary<TK, TV> dictB)
            where TV : IEquatable<TV>
        {
            if (dictA == null)
                throw new ArgumentNullException(nameof(dictA));
            if (dictB == null)
                throw new ArgumentNullException(nameof(dictB));
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

        /// <summary>
        ///     Performs a binary search for the specified key on a <see cref="SortedList&lt;TK,TV&gt;"/>. When no match
        ///     exists, returns the nearest indices for interpolation/extrapolation purposes.</summary>
        /// <remarks>
        ///     If an exact match exists, index1 == index2 == the index of the match. If an exact match is not found, index1
        ///     &lt; index2. If the key is less than every key in the list, index1 is int.MinValue and index2 is 0. If it's
        ///     greater than every key, index1 = last item index and index2 = int.MaxValue. Otherwise index1 and index2 are
        ///     the indices of the items that would surround the key were it present in the list.</remarks>
        /// <param name="list">
        ///     List to operate on.</param>
        /// <param name="key">
        ///     The key to look for.</param>
        /// <param name="index1">
        ///     Receives the value of the first index (see remarks).</param>
        /// <param name="index2">
        ///     Receives the value of the second index (see remarks).</param>
        public static void BinarySearch<TK, TV>(this SortedList<TK, TV> list, TK key, out int index1, out int index2)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            if (key == null)
                throw new ArgumentNullException(nameof(key), "Null values cannot be used for keys in SortedList.");

            var keys = list.Keys;
            var comparer = Comparer<TK>.Default;

            int imin = 0;
            int imax = (0 + keys.Count) - 1;
            while (imin <= imax)
            {
                int inew = imin + ((imax - imin) >> 1);

                int cmp_res;
                try { cmp_res = comparer.Compare(keys[inew], key); }
                catch (Exception exception) { throw new InvalidOperationException("SortedList.BinarySearch could not compare keys due to a comparer exception.", exception); }

                if (cmp_res == 0)
                {
                    index1 = index2 = inew;
                    return;
                }
                else if (cmp_res < 0)
                {
                    imin = inew + 1;
                }
                else
                {
                    imax = inew - 1;
                }
            }

            index1 = imax; // we know that imax + 1 == imin
            index2 = imin;
            if (imax < 0)
                index1 = int.MinValue;
            if (imin >= keys.Count)
                index2 = int.MaxValue;
        }

        /// <summary>
        ///     Gets a value from a dictionary by key. If the key does not exist in the dictionary, the default value is
        ///     returned instead.</summary>
        /// <param name="dict">
        ///     Dictionary to operate on.</param>
        /// <param name="key">
        ///     Key to look up.</param>
        /// <param name="defaultVal">
        ///     Value to return if <paramref name="key"/> is not contained in the dictionary.</param>
        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultVal)
        {
            if (dict == null)
                throw new ArgumentNullException(nameof(dict));
            if (key == null)
                throw new ArgumentNullException(nameof(key), "Null values cannot be used for keys in dictionaries.");
            return dict.TryGetValue(key, out var value) ? value : defaultVal;
        }

        /// <summary>
        ///     Gets a value from a dictionary by key. If the key does not exist in the dictionary, the default value is
        ///     returned instead.</summary>
        /// <param name="dict">
        ///     Dictionary to operate on.</param>
        /// <param name="key">
        ///     Key to look up.</param>
        /// <param name="defaultVal">
        ///     Value to return if <paramref name="key"/> is not contained in the dictionary.</param>
        public static TValue? Get<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue? defaultVal = null) where TValue : struct
        {
            if (dict == null)
                throw new ArgumentNullException(nameof(dict));
            if (key == null)
                throw new ArgumentNullException(nameof(key), "Null values cannot be used for keys in dictionaries.");
            return dict.TryGetValue(key, out var value) ? (TValue?) value : defaultVal;
        }

        /// <summary>
        ///     Gets a value from a two-level dictionary by key. If the keys don’t exist in the dictionary, the default value
        ///     is returned instead.</summary>
        /// <param name="dict">
        ///     Dictionary to operate on.</param>
        /// <param name="key1">
        ///     Key to look up in the first level.</param>
        /// <param name="key2">
        ///     Key to look up in the second level.</param>
        /// <param name="defaultVal">
        ///     Value to return if key1 or key2 is not contained in the relevant dictionary.</param>
        public static TValue Get<TKey1, TKey2, TValue>(this IDictionary<TKey1, Dictionary<TKey2, TValue>> dict, TKey1 key1, TKey2 key2, TValue defaultVal)
        {
            if (dict == null)
                throw new ArgumentNullException(nameof(dict));
            if (key1 == null)
                throw new ArgumentNullException(nameof(key1), "Null values cannot be used for keys in dictionaries.");
            if (key2 == null)
                throw new ArgumentNullException(nameof(key2), "Null values cannot be used for keys in dictionaries.");

            return dict.TryGetValue(key1, out var innerDic) && innerDic.TryGetValue(key2, out var value) ? value : defaultVal;
        }

        /// <summary>
        ///     Converts an <c>IEnumerable&lt;KeyValuePair&lt;TKey, TValue&gt;&gt;</c> into a <c>Dictionary&lt;TKey,
        ///     TValue&gt;</c>.</summary>
        /// <param name="source">
        ///     Source collection to convert to a dictionary.</param>
        /// <param name="comparer">
        ///     An optional equality comparer to compare keys.</param>
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey> comparer = null)
        {
            return source.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, comparer ?? EqualityComparer<TKey>.Default);
        }

        /// <summary>
        ///     Similar to <see cref="string.Substring(int)"/>, but for arrays. Returns a new array containing all items from
        ///     the specified <paramref name="startIndex"/> onwards.</summary>
        /// <remarks>
        ///     Returns a new copy of the array even if <paramref name="startIndex"/> is 0.</remarks>
        public static T[] Subarray<T>(this T[] array, int startIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            return Subarray(array, startIndex, array.Length - startIndex);
        }

        /// <summary>
        ///     Similar to <see cref="string.Substring(int,int)"/>, but for arrays. Returns a new array containing <paramref
        ///     name="length"/> items from the specified <paramref name="startIndex"/> onwards.</summary>
        /// <remarks>
        ///     Returns a new copy of the array even if <paramref name="startIndex"/> is 0 and <paramref name="length"/> is
        ///     the length of the input array.</remarks>
        public static T[] Subarray<T>(this T[] array, int startIndex, int length)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), "startIndex cannot be negative.");
            if (length < 0 || startIndex + length > array.Length)
                throw new ArgumentOutOfRangeException(nameof(length), "length cannot be negative or extend beyond the end of the array.");
            T[] result = new T[length];
            Array.Copy(array, startIndex, result, 0, length);
            return result;
        }

        /// <summary>
        ///     Similar to <see cref="string.Remove(int)"/>, but for arrays. Returns a new array containing only the items
        ///     before the specified <paramref name="startIndex"/>.</summary>
        /// <remarks>
        ///     Returns a new copy of the array even if <paramref name="startIndex"/> is the length of the array.</remarks>
        public static T[] Remove<T>(this T[] array, int startIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), "startIndex cannot be negative.");
            if (startIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex), "startIndex cannot be greater than the length of the array.");
            T[] result = new T[startIndex];
            Array.Copy(array, 0, result, 0, startIndex);
            return result;
        }

        /// <summary>
        ///     Similar to <see cref="string.Remove(int,int)"/>, but for arrays. Returns a new array containing everything
        ///     except the <paramref name="length"/> items starting from the specified <paramref name="startIndex"/>.</summary>
        /// <remarks>
        ///     Returns a new copy of the array even if <paramref name="length"/> is 0.</remarks>
        public static T[] Remove<T>(this T[] array, int startIndex, int length)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), "startIndex cannot be negative.");
            if (length < 0 || startIndex + length > array.Length)
                throw new ArgumentOutOfRangeException(nameof(length), "length cannot be negative or extend beyond the end of the array.");
            T[] result = new T[array.Length - length];
            Array.Copy(array, 0, result, 0, startIndex);
            Array.Copy(array, startIndex + length, result, startIndex, array.Length - length - startIndex);
            return result;
        }

        /// <summary>
        ///     Similar to <see cref="string.Insert(int, string)"/>, but for arrays. Returns a new array with the <paramref
        ///     name="values"/> inserted starting from the specified <paramref name="startIndex"/>.</summary>
        /// <remarks>
        ///     Returns a new copy of the array even if <paramref name="values"/> is empty.</remarks>
        public static T[] Insert<T>(this T[] array, int startIndex, params T[] values)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            if (startIndex < 0 || startIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex), "startIndex must be between 0 and the size of the input array.");
            T[] result = new T[array.Length + values.Length];
            Array.Copy(array, 0, result, 0, startIndex);
            Array.Copy(values, 0, result, startIndex, values.Length);
            Array.Copy(array, startIndex, result, startIndex + values.Length, array.Length - startIndex);
            return result;
        }

        /// <summary>
        ///     Similar to <see cref="string.Insert(int, string)"/>, but for arrays and for a single value. Returns a new
        ///     array with the <paramref name="value"/> inserted at the specified <paramref name="startIndex"/>.</summary>
        public static T[] Insert<T>(this T[] array, int startIndex, T value)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (startIndex < 0 || startIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex), "startIndex must be between 0 and the size of the input array.");
            T[] result = new T[array.Length + 1];
            Array.Copy(array, 0, result, 0, startIndex);
            result[startIndex] = value;
            Array.Copy(array, startIndex, result, startIndex + 1, array.Length - startIndex);
            return result;
        }

        /// <summary>
        ///     Determines whether a subarray within the current array is equal to the specified other array.</summary>
        /// <param name="sourceArray">
        ///     First array to examine.</param>
        /// <param name="sourceStartIndex">
        ///     Start index of the subarray within the first array to compare.</param>
        /// <param name="otherArray">
        ///     Array to compare the subarray against.</param>
        /// <param name="comparer">
        ///     Optional equality comparer.</param>
        /// <returns>
        ///     True if the current array contains the specified subarray at the specified index; false otherwise.</returns>
        public static bool SubarrayEquals<T>(this T[] sourceArray, int sourceStartIndex, T[] otherArray, IEqualityComparer<T> comparer = null)
        {
            if (otherArray == null)
                throw new ArgumentNullException(nameof(otherArray));
            return SubarrayEquals(sourceArray, sourceStartIndex, otherArray, 0, otherArray.Length, comparer);
        }

        /// <summary>
        ///     Determines whether the two arrays contain the same content in the specified location.</summary>
        /// <param name="sourceArray">
        ///     First array to examine.</param>
        /// <param name="sourceStartIndex">
        ///     Start index of the subarray within the first array to compare.</param>
        /// <param name="otherArray">
        ///     Second array to examine.</param>
        /// <param name="otherStartIndex">
        ///     Start index of the subarray within the second array to compare.</param>
        /// <param name="length">
        ///     Length of the subarrays to compare.</param>
        /// <param name="comparer">
        ///     Optional equality comparer.</param>
        /// <returns>
        ///     True if the two arrays contain the same subarrays at the specified indexes; false otherwise.</returns>
        public static bool SubarrayEquals<T>(this T[] sourceArray, int sourceStartIndex, T[] otherArray, int otherStartIndex, int length, IEqualityComparer<T> comparer = null)
        {
            if (sourceArray == null)
                throw new ArgumentNullException(nameof(sourceArray));
            if (sourceStartIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(sourceStartIndex), "The sourceStartIndex argument must be non-negative.");
            if (otherArray == null)
                throw new ArgumentNullException(nameof(otherArray));
            if (otherStartIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(otherStartIndex), "The otherStartIndex argument must be non-negative.");
            if (length < 0 || sourceStartIndex + length > sourceArray.Length || otherStartIndex + length > otherArray.Length)
                throw new ArgumentOutOfRangeException(nameof(length), "The length argument must be non-negative and must be such that both subarrays are within the bounds of the respective source arrays.");

            if (comparer == null)
                comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < length; i++)
                if (!comparer.Equals(sourceArray[sourceStartIndex + i], otherArray[otherStartIndex + i]))
                    return false;
            return true;
        }

        /// <summary>
        ///     Searches the current array for a specified subarray and returns the index of the first occurrence, or -1 if
        ///     not found.</summary>
        /// <param name="sourceArray">
        ///     Array in which to search for the subarray.</param>
        /// <param name="findWhat">
        ///     Subarray to search for.</param>
        /// <param name="comparer">
        ///     Optional equality comparer.</param>
        /// <returns>
        ///     The index of the first match, or -1 if no match is found.</returns>
        public static int IndexOfSubarray<T>(this T[] sourceArray, T[] findWhat, IEqualityComparer<T> comparer = null)
        {
            if (sourceArray == null)
                throw new ArgumentNullException(nameof(sourceArray));
            if (findWhat == null)
                throw new ArgumentNullException(nameof(findWhat));

            for (int i = 0; i <= sourceArray.Length; i++)
                if (sourceArray.SubarrayEquals(i, findWhat, 0, findWhat.Length, comparer))
                    return i;
            return -1;
        }

        /// <summary>
        ///     Searches the current array for a specified subarray and returns the index of the first occurrence, or -1 if
        ///     not found.</summary>
        /// <param name="sourceArray">
        ///     Array in which to search for the subarray.</param>
        /// <param name="findWhat">
        ///     Subarray to search for.</param>
        /// <param name="startIndex">
        ///     Index in <paramref name="sourceArray"/> at which to start searching.</param>
        /// <param name="sourceLength">
        ///     Maximum length of the source array to search starting from <paramref name="startIndex"/>. The greatest index
        ///     that can be returned is this minus the length of <paramref name="findWhat"/> plus <paramref
        ///     name="startIndex"/>.</param>
        /// <param name="comparer">
        ///     Optional equality comparer.</param>
        /// <returns>
        ///     The index of the first match, or -1 if no match is found.</returns>
        public static int IndexOfSubarray<T>(this T[] sourceArray, T[] findWhat, int startIndex, int? sourceLength = null, IEqualityComparer<T> comparer = null)
        {
            if (sourceArray == null)
                throw new ArgumentNullException(nameof(sourceArray));
            if (findWhat == null)
                throw new ArgumentNullException(nameof(findWhat));
            if (startIndex < 0 || startIndex > sourceArray.Length)
                throw new ArgumentOutOfRangeException("startIndex");
            if (sourceLength != null && (sourceLength < 0 || sourceLength + startIndex > sourceArray.Length))
                throw new ArgumentOutOfRangeException("sourceLength");

            var maxIndex = (sourceLength == null ? sourceArray.Length : startIndex + sourceLength.Value) - findWhat.Length;
            for (int i = startIndex; i <= maxIndex; i++)
                if (sourceArray.SubarrayEquals(i, findWhat, 0, findWhat.Length, comparer))
                    return i;
            return -1;
        }

        /// <summary>
        ///     Creates a new dictionary containing the union of the key/value pairs contained in the specified dictionaries.
        ///     Keys in <paramref name="second"/> overwrite keys in <paramref name="first"/>.</summary>
        public static IDictionary<TKey, TValue> CopyMerge<TKey, TValue>(this IDictionary<TKey, TValue> first, IDictionary<TKey, TValue> second)
        {
            if (first == null)
                throw new ArgumentNullException(nameof(first));
            if (second == null)
                throw new ArgumentNullException(nameof(second));
            Dictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>(first);
            foreach (var kvp in second)
                dict.Add(kvp.Key, kvp.Value);
            return dict;
        }

        /// <summary>
        ///     Removes all entries from a dictionary that satisfy a specified predicate.</summary>
        /// <typeparam name="TKey">
        ///     Type of the keys in the dictionary.</typeparam>
        /// <typeparam name="TVal">
        ///     Type of the values in the dictionary.</typeparam>
        /// <param name="dict">
        ///     Dictionary to operate on.</param>
        /// <param name="predicate">
        ///     Specifies a predicate that determines which entries should be removed from the dictionary.</param>
        public static void RemoveAll<TKey, TVal>(this IDictionary<TKey, TVal> dict, Func<KeyValuePair<TKey, TVal>, bool> predicate)
        {
            foreach (var kvp in dict.Where(kvp => predicate(kvp)).ToArray())
                dict.Remove(kvp.Key);
        }

        /// <summary>
        ///     Removes all entries from a dictionary whose keys satisfy a specified predicate.</summary>
        /// <typeparam name="TKey">
        ///     Type of the keys in the dictionary.</typeparam>
        /// <typeparam name="TVal">
        ///     Type of the values in the dictionary.</typeparam>
        /// <param name="dict">
        ///     Dictionary to operate on.</param>
        /// <param name="predicate">
        ///     Specifies a predicate that determines which entries should be removed from the dictionary.</param>
        public static void RemoveAllByKey<TKey, TVal>(this IDictionary<TKey, TVal> dict, Func<TKey, bool> predicate)
        {
            foreach (var kvp in dict.Where(kvp => predicate(kvp.Key)).ToArray())
                dict.Remove(kvp.Key);
        }

        /// <summary>
        ///     Removes all entries from a dictionary whose values satisfy a specified predicate.</summary>
        /// <typeparam name="TKey">
        ///     Type of the keys in the dictionary.</typeparam>
        /// <typeparam name="TVal">
        ///     Type of the values in the dictionary.</typeparam>
        /// <param name="dict">
        ///     Dictionary to operate on.</param>
        /// <param name="predicate">
        ///     Specifies a predicate that determines which entries should be removed from the dictionary.</param>
        public static void RemoveAllByValue<TKey, TVal>(this IDictionary<TKey, TVal> dict, Func<TVal, bool> predicate)
        {
            foreach (var kvp in dict.Where(kvp => predicate(kvp.Value)).ToArray())
                dict.Remove(kvp.Key);
        }

        /// <summary>
        ///     Enqueues several values into a <see cref="Queue&lt;T&gt;"/>.</summary>
        /// <typeparam name="T">
        ///     Type of the elements in the queue.</typeparam>
        /// <param name="queue">
        ///     Queue to insert items into.</param>
        /// <param name="values">
        ///     Values to enqueue.</param>
        public static void EnqueueRange<T>(this Queue<T> queue, IEnumerable<T> values)
        {
            foreach (var value in values)
                queue.Enqueue(value);
        }

        /// <summary>
        ///     Adds several values into a <see cref="HashSet&lt;T&gt;"/>.</summary>
        /// <typeparam name="T">
        ///     Type of the elements in the hash set.</typeparam>
        /// <param name="set">
        ///     The set to add the items to.</param>
        /// <param name="values">
        ///     Values to add.</param>
        public static void AddRange<T>(this HashSet<T> set, IEnumerable<T> values)
        {
            foreach (var value in values)
                set.Add(value);
        }

        /// <summary>
        ///     Removes several values from a <see cref="List&lt;T&gt;"/>.</summary>
        /// <typeparam name="T">
        ///     Type of the elements in the list.</typeparam>
        /// <param name="list">
        ///     The list to remove the items from.</param>
        /// <param name="values">
        ///     Values to remove.</param>
        public static void RemoveRange<T>(this List<T> list, IEnumerable<T> values)
        {
            foreach (var value in values)
                list.Remove(value);
        }

        /// <summary>
        ///     Projects each element of a sequence into a new form.</summary>
        /// <typeparam name="TInput">
        ///     The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TResult">
        ///     The type of the value returned by <paramref name="selector"/>.</typeparam>
        /// <param name="source">
        ///     A list of values to invoke the transform function on.</param>
        /// <param name="selector">
        ///     A transform function to apply to each element.</param>
        /// <returns>
        ///     A collection whose elements are the result of invoking the transform function on each element of <paramref
        ///     name="source"/>.</returns>
        /// <remarks>
        ///     This method replaces <see cref="Enumerable.Select{TSource, TResult}(IEnumerable{TSource}, Func{TSource,
        ///     TResult})"/> for the case where the input is an <see cref="IList{T}"/> with an implementation that makes a
        ///     subsequent <c>ToArray()</c> or <c>ToList()</c> run 15% faster.</remarks>
        public static ListSelectIterator<TInput, TResult> Select<TInput, TResult>(this IList<TInput> source, Func<TInput, TResult> selector)
        {
            return new ListSelectIterator<TInput, TResult>(source, selector);
        }

        /// <summary>
        ///     Inverts the order of the elements in a sequence.</summary>
        /// <typeparam name="TInput">
        ///     The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">
        ///     A list of values to reverse.</param>
        /// <returns>
        ///     A list whose elements correspond to those of the input sequence in reverse order.</returns>
        /// <remarks>
        ///     This method replaces <see cref="Enumerable.Reverse{TSource}(IEnumerable{TSource})"/> for the case where the
        ///     input is an <see cref="IList{T}"/> with an implementation that makes a subsequent <c>ToArray()</c> or
        ///     <c>ToList()</c> run 15% faster.</remarks>
        public static ListSelectIterator<TInput, TInput> Reverse<TInput>(this IList<TInput> source)
        {
            return new ListSelectIterator<TInput, TInput>(source, x => x, true);
        }

        /// <summary>Reverses an array in-place and returns the same array.</summary>
        public static T[] ReverseInplace<T>(this T[] input)
        {
            Array.Reverse(input);
            return input;
        }

        /// <summary>
        ///     Pops the specified number of elements from the stack. There must be at least that many items on the stack,
        ///     otherwise an exception is thrown.</summary>
        public static void Pop<T>(this Stack<T> stack, int count)
        {
            for (int i = 0; i < count; i++)
                stack.Pop();
        }
    }

    /// <summary>
    ///     Provides the implementation for <see cref="CollectionExtensions.Select{TInput,TResult}"/>.</summary>
    /// <typeparam name="TInput">
    ///     The type of the elements of the original collection.</typeparam>
    /// <typeparam name="TResult">
    ///     The type of the value returned by the selector function.</typeparam>
#if EXPORT_UTIL
    public
#endif
    sealed class ListSelectIterator<TInput, TResult> : IEnumerable<TResult>
    {
        private readonly IList<TInput> _source;
        private readonly Func<TInput, TResult> _selector;
        private readonly bool _reversed;

        /// <summary>
        ///     Constructor.</summary>
        /// <param name="source">
        ///     A list of values to invoke the transform function on.</param>
        /// <param name="selector">
        ///     A transform function to apply to each element.</param>
        /// <param name="reversed">
        ///     Specifies whether or not to reverse the order of elements.</param>
        public ListSelectIterator(IList<TInput> source, Func<TInput, TResult> selector, bool reversed = false)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _selector = selector;
            _reversed = reversed;
        }

        /// <summary>Returns an enumerator to iterate over the collection.</summary>
        public IEnumerator<TResult> GetEnumerator()
        {
            var len = _source.Count;
            for (int i = 0; i < len; i++)
                yield return _selector(_source[_reversed ? len - 1 - i : i]);
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        ///     Creates an array from a projected list.</summary>
        /// <remarks>
        ///     This implementation fulfills the same function as <c>Enumerable.ToArray()</c>, but is 15% faster.</remarks>
        public TResult[] ToArray()
        {
            var len = _source.Count;
            var arr = new TResult[len];
            for (int i = 0; i < len; i++)
                arr[i] = _selector(_source[_reversed ? len - 1 - i : i]);
            return arr;
        }

        /// <summary>
        ///     Creates a new list from a projected list.</summary>
        /// <remarks>
        ///     This implementation fulfills the same function as <c>Enumerable.ToList()</c>, but is 15% faster.</remarks>
        public List<TResult> ToList()
        {
            var len = _source.Count;
            var list = new List<TResult>(len);
            for (int i = 0; i < len; i++)
                list.Add(_selector(_source[_reversed ? len - 1 - i : i]));
            return list;
        }

        /// <summary>
        ///     Projects each element of a sequence into a new form.</summary>
        /// <typeparam name="TNewResult">
        ///     The type of the value returned by <paramref name="selector"/>.</typeparam>
        /// <param name="selector">
        ///     A transform function to apply to each element.</param>
        /// <returns>
        ///     A collection whose elements are the result of invoking the transform function on each element of the current
        ///     projected list.</returns>
        /// <remarks>
        ///     This method replaces <c>IEnumerable{T}.Select{TSource,
        ///     TResult}(IEnumerable{TSource},Func{TSource,int,TResult})</c> for the case where the input is a
        ///     <c>ListSelectIterator&lt;TInput, TResult&gt;</c> with an implementation that makes a subsequent
        ///     <c>ToArray()</c> or <c>ToList()</c> run 15% faster.</remarks>
        public ListSelectIterator<TInput, TNewResult> Select<TNewResult>(Func<TResult, TNewResult> selector)
        {
            return new ListSelectIterator<TInput, TNewResult>(_source, input => selector(_selector(input)), _reversed);
        }

        /// <summary>
        ///     Inverts the order of the elements in a sequence.</summary>
        /// <returns>
        ///     A list whose elements correspond to those of the input sequence in reverse order.</returns>
        /// <remarks>
        ///     This method replaces <see cref="Enumerable.Reverse{TSource}(IEnumerable{TSource})"/> for the case where the
        ///     input is an <see cref="IList{T}"/> with an implementation that makes a subsequent <c>ToArray()</c> or
        ///     <c>ToList()</c> run 15% faster.</remarks>
        public ListSelectIterator<TInput, TResult> Reverse()
        {
            return new ListSelectIterator<TInput, TResult>(_source, _selector, !_reversed);
        }
    }
}
