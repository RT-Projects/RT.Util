using System;
using System.Collections.Generic;

namespace RT.Util
{
    /// <summary>
    /// This class offers some generic static functions which are hard to categorize
    /// under any more specific classes.
    /// </summary>
    public static partial class Ut
    {
        /// <summary>
        /// Compares two arrays with the elements of the specified type for equality.
        /// Arrays are equal if both are null, or if all elements are equal.
        /// </summary>
        public static bool ArraysEqual<T>(T[] Arr1, T[] Arr2) where T: IEquatable<T>
        {
            if (Arr1 == null && Arr2 == null)
                return true;
            else if (Arr1 == null || Arr2 == null)
                return false;
            else if (Arr1.Length != Arr2.Length)
                return false;

            for (int i=0; i<Arr1.Length; i++)
                if (Arr1[i].Equals(Arr2[i]))
                    return false;
            return true;
        }

        /// <summary>
        /// Counts the number of occurrences of string in another string
        /// </summary>
        /// <param name="in_string">Main string</param>
        /// <param name="to_be_counted">String to be counted</param>
        /// <returns>Number of occurrences of to_be_counted</returns>
        public static int CountStrings(string in_string, string to_be_counted)
        {
            int result = -1;
            int last = -1;
            do
            {
                result++;
                last = in_string.IndexOf(to_be_counted, last+1);
            } while (last != -1);
            return result;
        }

        /// <summary>
        /// Converts file size in bytes to a string in bytes, kbytes, Mbytes
        /// or Gbytes accordingly. The suffix appended is kB, MB or GB.
        /// </summary>
        /// <param name="size">Size in bytes</param>
        /// <returns>Converted string</returns>
        public static string SizeToString(long size)
        {
            if (size == 0)
            {
                return "0";
            }
            else if (size < 1024)
            {
                return size.ToString("#,###");
            }
            else if (size < 1024 * 1024)
            {
                return (size / 1024d).ToString("#,###.## kB");
            }
            else if (size < 1024 * 1024 * 1024)
            {
                return (size / (1024d * 1024d)).ToString("#,###.## MB");
            }
            else
            {
                return (size / (1024d * 1024d * 1024d)).ToString("#,###.## GB");
            }
        }

        /// <summary>
        /// Returns an IEnumerable containing all integers between the specified First and Last integers (all inclusive).
        /// </summary>
        /// <param name="First">First integer to return.</param>
        /// <param name="Last">Last integer to return.</param>
        /// <returns>An IEnumerable containing all integers between the specified First and Last integers (all inclusive).</returns>
        public static IEnumerable<int> Range(int First, int Last)
        {
            for (int i = First; i <= Last; i++)
                yield return i;
        }
    }
}
