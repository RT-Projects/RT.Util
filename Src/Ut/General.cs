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
    }
}
