using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RT.Util.ExtensionMethods
{
    /// <summary>
    /// Provides extension methods on numeric types.
    /// </summary>
    public static class NumericExtensions
    {
        /// <summary>
        /// Clips this value to the range defined by <paramref name="minimumValue"/> and <paramref name="maximumValue"/>.
        /// The returned number will be no less than the minimum value and no greater than the maximum value. Throws
        /// an exception if min value is greater than the max value.
        /// </summary>
        public static int Clip(this int value, int minimumValue, int maximumValue)
        {
            if (minimumValue > maximumValue)
                throw new ArgumentException("Clip: minimumValue is greater than maximumValue");
            return value <= minimumValue ? minimumValue : value >= maximumValue ? maximumValue : value;
        }

        /// <summary>Clips this value so that it is no less than the minimum value specified.</summary>
        public static int ClipMin(this int value, int minimumValue) { return value <= minimumValue ? minimumValue : value; }
        /// <summary>Clips this value so that it is no greater than the maximum value specified.</summary>
        public static int ClipMax(this int value, int maxnimumValue) { return value >= maxnimumValue ? maxnimumValue : value; }

        /// <summary>
        /// Clips this value to the range defined by <paramref name="minimumValue"/> and <paramref name="maximumValue"/>.
        /// The returned number will be no less than the minimum value and no greater than the maximum value. Throws
        /// an exception if min value is greater than the max value.
        /// </summary>
        public static double Clip(this double value, double minimumValue, double maximumValue)
        {
            if (minimumValue > maximumValue)
                throw new ArgumentException("Clip: minimumValue is greater than maximumValue");
            return value <= minimumValue ? minimumValue : value >= maximumValue ? maximumValue : value;
        }

        /// <summary>Clips this value so that it is no less than the minimum value specified.</summary>
        public static double ClipMin(this double value, double minimumValue) { return value <= minimumValue ? minimumValue : value; }
        /// <summary>Clips this value so that it is no greater than the maximum value specified.</summary>
        public static double ClipMax(this double value, double maxnimumValue) { return value >= maxnimumValue ? maxnimumValue : value; }

        /// <summary>
        /// Clips this value to the range defined by <paramref name="minimumValue"/> and <paramref name="maximumValue"/>.
        /// The returned number will be no less than the minimum value and no greater than the maximum value. Throws
        /// an exception if min value is greater than the max value.
        /// </summary>
        public static decimal Clip(this decimal value, decimal minimumValue, decimal maximumValue)
        {
            if (minimumValue > maximumValue)
                throw new ArgumentException("Clip: minimumValue is greater than maximumValue");
            return value <= minimumValue ? minimumValue : value >= maximumValue ? maximumValue : value;
        }

        /// <summary>Clips this value so that it is no less than the minimum value specified.</summary>
        public static decimal ClipMin(this decimal value, decimal minimumValue) { return value <= minimumValue ? minimumValue : value; }
        /// <summary>Clips this value so that it is no greater than the maximum value specified.</summary>
        public static decimal ClipMax(this decimal value, decimal maxnimumValue) { return value >= maxnimumValue ? maxnimumValue : value; }

        /// <summary>
        /// Attempts to parse this string as an int, returning null if the parse fails.
        /// </summary>
        public static int? TryParseAsInt(this string value)
        {
            int result;
            if (int.TryParse(value, out result))
                return result;
            else
                return null;
        }
    }
}
