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
    }
}
