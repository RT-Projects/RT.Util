using System;
using System.Linq;
using System.Collections.Generic;
using RT.Util.Collections;
using RT.Util.ExtensionMethods;

namespace RT.Util
{
    /// <summary>
    /// This class offers some generic static functions which are hard to categorize
    /// under any more specific classes.
    /// </summary>
    public static partial class Ut
    {
        /// <summary>
        /// Converts file size in bytes to a string in bytes, kbytes, Mbytes
        /// or Gbytes accordingly. The suffix appended is kB, MB or GB.
        /// </summary>
        /// <param name="size">Size in bytes</param>
        /// <returns>Converted string</returns>
        public static string SizeToString(long size)
        {
            if (size == 0)
                return "0";
            else if (size < 1024)
                return size.ToString("#,###");
            else if (size < 1024 * 1024)
                return (size / 1024d).ToString("#,###.## kB");
            else if (size < 1024 * 1024 * 1024)
                return (size / (1024d * 1024d)).ToString("#,###.## MB");
            else
                return (size / (1024d * 1024d * 1024d)).ToString("#,###.## GB");
        }

        /// <summary>
        /// Returns the smaller of the two IComparable values. If the values are
        /// equal, returns the first one.
        /// </summary>
        public static T Min<T>(T val1, T val2) where T : IComparable<T>
        {
            return val1.CompareTo(val2) <= 0 ? val1 : val2;
        }

        /// <summary>
        /// Returns the smaller of the three IComparable values. If two values are
        /// equal, returns the earlier one.
        /// </summary>
        public static T Min<T>(T val1, T val2, T val3) where T : IComparable<T>
        {
            T c1 = val1.CompareTo(val2) <= 0 ? val1 : val2;
            return c1.CompareTo(val3) <= 0 ? c1 : val3;
        }

        /// <summary>
        /// Returns the smallest of all arguments passed in. Uses the Linq .Min
        /// extension method to do the work.
        /// </summary>
        public static T Min<T>(params T[] args) where T : IComparable<T>
        {
            return args.Min();
        }

        /// <summary>
        /// Returns the larger of the two IComparable values. If the values are
        /// equal, returns the first one.
        /// </summary>
        public static T Max<T>(T val1, T val2) where T : IComparable<T>
        {
            return val1.CompareTo(val2) >= 0 ? val1 : val2;
        }

        /// <summary>
        /// Returns the larger of the three IComparable values. If two values are
        /// equal, returns the earlier one.
        /// </summary>
        public static T Max<T>(T val1, T val2, T val3) where T : IComparable<T>
        {
            T c1 = val1.CompareTo(val2) >= 0 ? val1 : val2;
            return c1.CompareTo(val3) >= 0 ? c1 : val3;
        }

        /// <summary>
        /// Returns the largest of all arguments passed in. Uses the Linq .Max
        /// extension method to do the work.
        /// </summary>
        public static T Max<T>(params T[] args) where T : IComparable<T>
        {
            return args.Max();
        }
    }
}
