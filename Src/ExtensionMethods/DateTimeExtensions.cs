using System;
using System.Text;
using System.Text.RegularExpressions;

namespace RT.Util.ExtensionMethods
{
    /// <summary>
    /// Provides extension methods on the <see cref="DateTime"/> type.
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Inexplicably, the DateTime type does not offer any way of retrieving the full precision
        /// of the underlying data other than via Ticks or the ToString method. This extension
        /// method fills in the void.
        /// </summary>
        public static int Nanosecond(this DateTime datetime)
        {
            return ((int) (datetime.Ticks % 10000000)) * 100;
        }

        #region ISO 8601 conversion

        /// <summary>
        /// Converts the specified DateTime to a string representing the datetime in
        /// ISO format. The resulting string holds all the information necessary to
        /// convert back to the original DateTime. Example string:
        /// "2007-12-31 21:00:00.0000000Z" - where the Z suffix indicates that this
        /// time is in UTC.
        /// </summary>
        public static string ToIsoStringFull(this DateTime datetime)
        {
            switch (datetime.Kind)
            {
                case DateTimeKind.Utc:
                    return datetime.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss'.'fffffffZ");
                case DateTimeKind.Local:
                    return datetime.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss'.'fffffffzzz");
                case DateTimeKind.Unspecified:
                    return datetime.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss'.'fffffff");
                default:
                    // to keep the compiler happy
                    throw new Exception("Unexpected DateTime.Kind");
            }
        }

        /// <summary>
        /// Converts the specified DateTime to a string representing the datetime in
        /// ISO format. The resulting string holds all the information necessary to
        /// convert back to the original DateTime, but unlike <see cref="ToIsoStringFull"/>,
        /// some of the redundant zeros are omitted (as permitted by the ISO format).
        /// </summary>
        /// <param name="datetime">The value to convert.</param>
        /// <param name="betweenDateAndTime">If set to space (the default), the "extended" (human-readable) format will be used.
        /// If set to any other character, will use the basic (no separators) format within date/time, and this character between them.
        /// Use 'T' or '-' to make it compatible with <see cref="TryParseIso"/></param>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>Example 1: "2007-12-31 21:00:15.993Z" - the micro/nanoseconds are all 0 but the milliseconds aren't</description></item>
        /// <item><description>Example 1: "2007-12-31 21:00:15Z" - the sub-second are all 0</description></item>
        /// <item><description>Example 2: "2007-12-31 21:15Z" - the seconds, nanoseconds are all 0</description></item>
        /// <item><description>Example 3: "2007-12-31Z" - the hours, minutes, seconds, nanoseconds are all 0</description></item>
        /// </list>
        /// <para>
        /// Note that in the first example the ISO format allows the minutes and seconds
        /// to be skipped as well - this is not implemented at the moment because the
        /// resulting string looks too ambiguous and hard to interpret.
        /// </para>
        /// </remarks>
        public static string ToIsoStringOptimal(this DateTime datetime, char betweenDateAndTime = ' ')
        {
            int fmt;
            if (datetime.Nanosecond() != 0)
            {
                if (datetime.Nanosecond() % 1000000 != 0)
                    fmt = 1; // everything
                else
                    fmt = 2; // up to milliseconds
            }
            else if (datetime.Second != 0)
                fmt = 3; // up to seconds
            else if (datetime.Minute != 0 || datetime.Hour != 0)
                fmt = 4; // up to minutes
            else
                fmt = 5; // up to days

            var result = new StringBuilder();
            result.AppendFormat("{0:0000}", datetime.Year);
            if (betweenDateAndTime == ' ') result.Append('-');
            result.AppendFormat("{0:00}", datetime.Month);
            if (betweenDateAndTime == ' ') result.Append('-');
            result.AppendFormat("{0:00}", datetime.Day);

            if (fmt < 5)
            {
                if (betweenDateAndTime == ' ') result.Append(betweenDateAndTime);
                result.AppendFormat("{0:00}", datetime.Hour);
                if (betweenDateAndTime == ' ') result.Append(':');
                result.AppendFormat("{0:00}", datetime.Minute);
                if (fmt < 4)
                {
                    if (betweenDateAndTime == ' ') result.Append(':');
                    result.AppendFormat("{0:00}", datetime.Second);
                    if (fmt == 2) result.AppendFormat(".{0:000}", datetime.Millisecond);
                    if (fmt == 1) result.AppendFormat(".{0:0000000}", datetime.Nanosecond() / 100);
                }
            }

            if (datetime.Kind == DateTimeKind.Utc)
                result.Append('Z');
            else if (datetime.Kind == DateTimeKind.Local)
            {
                var suffix = datetime.ToString("zzz");
                if (suffix.EndsWith("00"))
                    suffix = suffix.Substring(0, suffix.Length - 3);
                else if (betweenDateAndTime != ' ')
                    suffix = suffix.Replace(":", "");
                result.Append(suffix);
            }

            return result.ToString();
        }

        private static Regex _cachedIsoRegexBasic;
        private static Regex isoRegexBasic
        {
            get
            {
                if (_cachedIsoRegexBasic == null)
                    _cachedIsoRegexBasic = new Regex(@"^(?<yr>\d\d\d\d)(?<mo>\d\d)(?<da>\d\d)([T-](?<hr>\d\d)((?<mi>\d\d)((?<se>\d\d))?)?(?<frac>\.\d{1,7})?((?<tzz>Z)|(?<tzs>[+-])(?<tzh>\d\d)((?<tzm>\d\d))?)?)?$", RegexOptions.ExplicitCapture);
                return _cachedIsoRegexBasic;
            }
        }

        private static Regex _cachedIsoRegexExtended;
        private static Regex isoRegexExtended
        {
            get
            {
                if (_cachedIsoRegexExtended == null)
                    _cachedIsoRegexExtended = new Regex(@"^(?<yr>\d\d\d\d)(-(?<mo>\d\d)(-(?<da>\d\d)( (?<hr>\d\d)(:(?<mi>\d\d)(:(?<se>\d\d))?)?(?<frac>\.\d{1,7})?((?<tzz>Z)|(?<tzs>[+-])(?<tzh>\d\d)(:(?<tzm>\d\d))?)?)?)?)?$", RegexOptions.ExplicitCapture);
                return _cachedIsoRegexExtended;
            }
        }

        /// <summary>
        /// Attempts to parse the specified string as an ISO-formatted DateTime. The formats supported are guided by ISO-8601, but do not match
        /// it exactly. Strings with no timezone information are parsed into DateTimeKind.Unspecified.
        /// <para>ISO-8601 features not supported: day numbers; week numbers; time offsets; comma for decimal separation.</para>
        /// <para>Features supported not in ISO-8601: '-' separator for the basic format; date shortening.</para>
        /// </summary>
        public static bool TryParseIso(string str, out DateTime result)
        {
            result = default(DateTime);
            var match = isoRegexBasic.Match(str);
            if (!match.Success)
                match = isoRegexExtended.Match(str);
            if (!match.Success)
                return false;

            int yr = int.Parse(match.Groups["yr"].Value);
            int mo = match.Groups["mo"].Success ? int.Parse(match.Groups["mo"].Value) : 1;
            if (mo < 1 || mo > 12)
                return false;
            int da = match.Groups["da"].Success ? int.Parse(match.Groups["da"].Value) : 1;
            if (da < 1 || da > 31)
                return false;

            int hr = match.Groups["hr"].Success ? int.Parse(match.Groups["hr"].Value) : 0;
            if (hr > 24)
                return false;
            int mi = match.Groups["mi"].Success ? int.Parse(match.Groups["mi"].Value) : 0;
            if (mi > 59)
                return false;
            int se = match.Groups["se"].Success ? int.Parse(match.Groups["se"].Value) : 0;
            if (se > 59)
                return false;

            if (match.Groups["tzz"].Success)
            {
                try { result = new DateTime(yr, mo, da, hr, mi, se, DateTimeKind.Utc); }
                catch { return false; }
            }
            else if (!match.Groups["tzs"].Success)
            {
                try { result = new DateTime(yr, mo, da, hr, mi, se, DateTimeKind.Unspecified); }
                catch { return false; }
            }
            else
            {
                int tzh = int.Parse(match.Groups["tzh"].Value);
                if (tzh > 24)
                    return false;
                int tzm = match.Groups["tzm"].Success ? int.Parse(match.Groups["tzm"].Value) : 0;
                if (tzm > 59)
                    return false;

                int totalMinutes = tzh * 60 + tzm;
                if (match.Groups["tzs"].Value == "-")
                    totalMinutes = -totalMinutes;

                try { result = new DateTime(yr, mo, da, hr, mi, se, DateTimeKind.Utc); } // yes, UTC initially
                catch { return false; }
                result = (result - TimeSpan.FromMinutes(totalMinutes)).ToLocalTime();
            }

            if (match.Groups["frac"].Success)
            {
                int frac = int.Parse(match.Groups["frac"].Value.Substring(1).PadRight(7, '0')); // 7 is special because that makes this the number of ticks in a second for the second fraction
                if (!match.Groups["mi"].Success)
                    result = new DateTime(result.Ticks + frac * (TimeSpan.TicksPerHour / TimeSpan.TicksPerSecond), result.Kind);
                else if (!match.Groups["se"].Success)
                    result = new DateTime(result.Ticks + frac * (TimeSpan.TicksPerMinute / TimeSpan.TicksPerSecond), result.Kind);
                else
                    result = new DateTime(result.Ticks + frac, result.Kind);
            }

            return true;
        }

        /// <summary>Parse the specified string as an ISO-formatted DateTime. See <see cref="TryParseIso"/> for more info.</summary>
        public static DateTime ParseIso(string str)
        {
            DateTime result;
            if (!TryParseIso(str, out result))
                throw new FormatException("The string is not in a recognized date/time format.");
            return result;
        }

        #endregion

        /// <summary>
        /// Returns a copy of this DateTime, truncated to whole seconds. Useful with
        /// <see cref="ToIsoStringOptimal"/>.
        /// </summary>
        public static DateTime TruncatedToSeconds(this DateTime datetime)
        {
            return new DateTime(datetime.Ticks - datetime.Ticks % 10000000, datetime.Kind);
        }

        /// <summary>
        /// Returns a copy of this DateTime, truncated to whole days. Useful with
        /// <see cref="ToIsoStringOptimal"/>.
        /// </summary>
        public static DateTime TruncatedToDays(this DateTime datetime)
        {
            return new DateTime(datetime.Ticks - datetime.Ticks % 864000000000, datetime.Kind);
        }
    }
}
