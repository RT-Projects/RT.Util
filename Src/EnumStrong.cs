using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RT.Util
{
    /// <summary>
    /// Provides generic versions of some of the static methods of the <see cref="Enum"/> class.
    /// </summary>
    public static class EnumStrong
    {
#pragma warning disable 1591    // Missing XML comment for publicly visible type or member
        public static T Parse<T>(string value)
        {
            return (T) Enum.Parse(typeof(T), value);
        }

        public static T Parse<T>(string value, bool ignoreCase)
        {
            return (T) Enum.Parse(typeof(T), value, ignoreCase);
        }

        public static bool TryParse<T>(string value, out T result)
        {
            try { result = (T) Enum.Parse(typeof(T), value); return true; }
            catch { result = default(T); return false; }
        }

        public static bool TryParse<T>(string value, out T result, bool ignoreCase)
        {
            try { result = (T) Enum.Parse(typeof(T), value, ignoreCase); return true; }
            catch { result = default(T); return false; }
        }
#pragma warning restore 1591    // Missing XML comment for publicly visible type or member
    }
}
