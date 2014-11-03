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
                throw new ArgumentException("Clip: minimumValue cannot be greater than maximumValue.", "maximumValue");
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
                throw new ArgumentException("Clip: minimumValue cannot be greater than maximumValue.", "maximumValue");
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
                throw new ArgumentException("Clip: minimumValue cannot be greater than maximumValue.", "maximumValue");
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

        /// <summary>
        ///     Converts the integer to a textual representation using English words. For example, 142.ToWords() is "one
        ///     hundred and forty-two".</summary>
        public static string ToWords(this int number)
        {
            if (number == 0)
                return "zero";

            if (number < 0)
                return "minus " + ToWords(-number);

            string words = "";

            if ((number / 1000000000) > 0)
            {
                words += " " + ToWords(number / 1000000000) + " billion";
                number %= 1000000000;
            }

            if ((number / 1000000) > 0)
            {
                words += " " + ToWords(number / 1000000) + " million";
                number %= 1000000;
            }

            if ((number / 1000) > 0)
            {
                words += " " + ToWords(number / 1000) + " thousand";
                number %= 1000;
            }

            if ((number / 100) > 0)
            {
                words += " " + ToWords(number / 100) + " hundred";
                number %= 100;
            }

            if (number > 0)
            {
                if (words != "")
                    words += " and";

                var unitsMap = new[] { null, "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
                var tensMap = new[] { null, "ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };

                if (number < 20)
                    words += " " + unitsMap[number];
                else
                {
                    words += " " + tensMap[number / 10];
                    if ((number % 10) > 0)
                        words += "-" + unitsMap[number % 10];
                }
            }

            return words.Substring(1);
        }
    }
}
