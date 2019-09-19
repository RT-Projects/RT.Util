using System;
using System.Text;
using System.Text.RegularExpressions;

namespace RT.Util.ExtensionMethods
{
    /// <summary>Defines one of several common date/time formats which are either ISO-8601 compatible or very slight deviations from it.</summary>
    public enum IsoDateFormat
    {
        /// <summary>A delimited, readable format. Known as "extended" in ISO-8601. Example: <c>2007-12-31 21:15</c>.</summary>
        HumanReadable,
        /// <summary>A non-delimited compact format. Known as "basic" in ISO-8601. Example: <c>20071231T2115</c>.</summary>
        Compact,
        /// <summary>A non-delimited compact format with '-' instead of 'T'. Not ISO-8601, but supported by <see cref="DateTimeExtensions.TryParseIso"/>. Example: <c>20071231-2115</c>.</summary>
        CompactReadable,
        /// <summary>A delimited, readable format without spaces usable in filenames. Not ISO-8601, and not supported by <see cref="DateTimeExtensions.TryParseIso"/>. Example: <c>2007.12.31-21.15</c>.</summary>
        FilenameReadable,
        /// <summary>The standard ISO-8601 format. Example: <c>2007-12-31T21:15</c>.</summary>
        Iso8601
    }

    /// <summary>Defines a precision for a date/time stamp.</summary>
    public enum IsoDatePrecision
    {
        /// <summary>Day precision: <c>2011-12-31</c></summary>
        Days = 10,
        /// <summary>Minute precision: <c>2011-12-31 18:03</c></summary>
        Minutes = 20,
        /// <summary>Second precision: <c>2011-12-31 18:03:15</c></summary>
        Seconds = 30,
        /// <summary>Millisecond precision: <c>2011-12-31 18:03:15.123</c></summary>
        Milliseconds = 40,
        /// <summary>The full .NET DateTime precision, which is seconds to 7 d.p. (100-nanosecond intervals): <c>2011-12-31 18:03:15.1234567</c></summary>
        Full = 50,
    }

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

        /// <summary>
        /// Returns a string representation of the date/time in an ISO-8601-like format. The date/time components are always ordered from
        /// largest (year) to smallest (nanoseconds), and they are always specified as a fixed-width numeric value. The separators between
        /// the parts can be customized.
        /// </summary>
        /// <param name="datetime">Date/time to convert.</param>
        /// <param name="precision">Which date/time components are to be included. The values are truncated, not rounded.</param>
        /// <param name="charInDate">The character to insert between years, months and days, or null for none.</param>
        /// <param name="charInTime">The character to insert between hours, minutes and seconds (including timezone offset), or null for none.</param>
        /// <param name="charBetween">The character to insert between the date and the time part, or null for none (which is never valid in ISO-8601).</param>
        /// <param name="includeTimezone">Specifies whether a suffix indicating date/time kind (local/utc/unspecified) and, for local times, a UTC offset, is appended.</param>
        public static string ToIsoStringCustom(this DateTime datetime, IsoDatePrecision precision = IsoDatePrecision.Full,
            char? charInDate = '-', char? charInTime = ':', char? charBetween = ' ', bool includeTimezone = false)
        {
            var result = new StringBuilder();
            result.AppendFormat("{0:0000}", datetime.Year);
            if (charInDate != null) result.Append(charInDate.Value);
            result.AppendFormat("{0:00}", datetime.Month);
            if (charInDate != null) result.Append(charInDate.Value);
            result.AppendFormat("{0:00}", datetime.Day);

            if (precision > IsoDatePrecision.Days)
            {
                if (charBetween != null) result.Append(charBetween.Value);
                result.AppendFormat("{0:00}", datetime.Hour);
                if (charInTime != null) result.Append(charInTime.Value);
                result.AppendFormat("{0:00}", datetime.Minute);
                if (precision > IsoDatePrecision.Minutes)
                {
                    if (charInTime != null) result.Append(charInTime.Value);
                    result.AppendFormat("{0:00}", datetime.Second);
                    if (precision == IsoDatePrecision.Milliseconds) result.AppendFormat(".{0:000}", datetime.Millisecond);
                    if (precision == IsoDatePrecision.Full) result.AppendFormat(".{0:0000000}", datetime.Nanosecond() / 100);
                }
            }

            if (includeTimezone)
            {
                if (datetime.Kind == DateTimeKind.Utc)
                    result.Append('Z');
                else if (datetime.Kind == DateTimeKind.Local)
                {
                    var offset = TimeZone.CurrentTimeZone.GetUtcOffset(datetime);
                    result.Append(offset >= TimeSpan.Zero ? "+" : "");
                    result.AppendFormat("{0:00}", offset.Hours);
                    if (offset.Minutes != 0)
                    {
                        if (charInTime != null) result.Append(charInTime.Value);
                        result.AppendFormat("{0:00}", offset.Minutes);
                    }
                }
            }

            return result.ToString();
        }

        /// <summary>Returns a string representation of the date/time in an ISO-8601 compatible (or very close) format.</summary>
        /// <param name="datetime">Date/time to convert.</param>
        /// <param name="precision">Which date/time components are to be included. The values are truncated, not rounded.</param>
        /// <param name="format">One of the several pre-defined formats to use.</param>
        /// <param name="includeTimezone">Specifies whether a suffix indicating date/time kind (local/utc/unspecified) and, for local times, a UTC offset, is appended.</param>
        public static string ToIsoString(this DateTime datetime, IsoDatePrecision precision = IsoDatePrecision.Seconds, IsoDateFormat format = IsoDateFormat.HumanReadable, bool includeTimezone = false)
        {
            switch (format)
            {
                case IsoDateFormat.HumanReadable: return datetime.ToIsoStringCustom(precision, charInDate: '-', charInTime: ':', charBetween: ' ', includeTimezone: includeTimezone);
                case IsoDateFormat.Compact: return datetime.ToIsoStringCustom(precision, charInDate: null, charInTime: null, charBetween: 'T', includeTimezone: includeTimezone);
                case IsoDateFormat.CompactReadable: return datetime.ToIsoStringCustom(precision, charInDate: null, charInTime: null, charBetween: '-', includeTimezone: includeTimezone);
                case IsoDateFormat.FilenameReadable: return datetime.ToIsoStringCustom(precision, charInDate: '.', charInTime: '.', charBetween: '-', includeTimezone: includeTimezone);
                case IsoDateFormat.Iso8601: return datetime.ToIsoStringCustom(precision, charInDate: '-', charInTime: ':', charBetween: 'T', includeTimezone: includeTimezone);
                default: throw new Exception("usbwdg");
            }
        }

        /// <summary>
        /// Returns a string representation of the date/time in an ISO-8601-like format. The function will
        /// omit higher-precision parts whose values are zeroes, as permitted by the standard.
        /// </summary>
        /// <param name="datetime">Date/time to convert.</param>
        /// <param name="format">One of the several pre-defined formats to use.</param>
        /// <param name="minPrecision">Minimum precision of the resulting string. The actual precision is determined by what's available in the date/time, bounded by this parameter.</param>
        /// <param name="maxPrecision">Maximum precision of the resulting string. Any higher-precision parts are truncated.</param>
        /// <param name="includeTimezone">Specifies whether a suffix indicating date/time kind (local/utc/unspecified) and, for local times, a UTC offset, is appended.</param>
        public static string ToIsoStringOptimal(this DateTime datetime, IsoDateFormat format = IsoDateFormat.HumanReadable,
            IsoDatePrecision minPrecision = IsoDatePrecision.Days, IsoDatePrecision maxPrecision = IsoDatePrecision.Full, bool includeTimezone = false)
        {
            if (minPrecision > maxPrecision)
                throw new ArgumentException("Minimum precision must not exceed maximum precision.", nameof(maxPrecision));
            IsoDatePrecision precision;
            if (datetime.Nanosecond() % 1000000 != 0 && maxPrecision > IsoDatePrecision.Milliseconds)
                precision = IsoDatePrecision.Full;
            else if (datetime.Millisecond != 0 && maxPrecision > IsoDatePrecision.Seconds)
                precision = IsoDatePrecision.Milliseconds;
            else if (datetime.Second != 0 && maxPrecision > IsoDatePrecision.Minutes)
                precision = IsoDatePrecision.Seconds;
            else if (datetime.Minute != 0 || datetime.Hour != 0 && maxPrecision > IsoDatePrecision.Days)
                precision = IsoDatePrecision.Minutes;
            else
                precision = IsoDatePrecision.Days;

            if (precision < minPrecision)
                precision = minPrecision;
            if (precision > maxPrecision)
                precision = maxPrecision;

            return datetime.ToIsoString(precision, format, includeTimezone: includeTimezone);
        }

        /// <summary>
        /// Returns a string representation of the date/time in an ISO-8601-like format. Use this if the result must be round-trippable
        /// without losing any information. The function will omit higher-precision parts whose values are zeroes, as permitted by the standard.
        /// </summary>
        /// <param name="datetime">Date/time to convert.</param>
        /// <param name="format">One of the several pre-defined formats to use.</param>
        /// <param name="minPrecision">Minimum precision of the resulting string. The actual precision is determined by what's available in the date/time, bounded by this parameter.</param>
        public static string ToIsoStringRoundtrip(this DateTime datetime, IsoDateFormat format = IsoDateFormat.HumanReadable, IsoDatePrecision minPrecision = IsoDatePrecision.Days)
        {
            return datetime.ToIsoStringOptimal(format, minPrecision: minPrecision, maxPrecision: IsoDatePrecision.Full, includeTimezone: true);
        }

        private static Regex _cachedIsoRegexBasic;
        private static Regex isoRegexBasic
        {
            get
            {
                if (_cachedIsoRegexBasic == null)
                    _cachedIsoRegexBasic = new Regex(@"^(?<yr>\d\d\d\d)(?<mo>\d\d)(?<da>\d\d)([T-](?<hr>\d\d)((?<mi>\d\d)((?<se>\d\d))?)?(?<frac>\.\d{1,7})?)?((?<tzz>Z)|(?<tzs>[+-])(?<tzh>\d\d)((?<tzm>\d\d))?)?$", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
                return _cachedIsoRegexBasic;
            }
        }

        private static Regex _cachedIsoRegexExtended;
        private static Regex isoRegexExtended
        {
            get
            {
                if (_cachedIsoRegexExtended == null)
                    _cachedIsoRegexExtended = new Regex(@"^(?<yr>\d\d\d\d)(-(?<mo>\d\d)(-(?<da>\d\d)( (?<hr>\d\d)(:(?<mi>\d\d)(:(?<se>\d\d))?)?(?<frac>\.\d{1,7})?)?)?)?((?<tzz>Z)|(?<tzs>[+-])(?<tzh>\d\d)(:(?<tzm>\d\d))?)?$", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
                return _cachedIsoRegexExtended;
            }
        }

        /// <summary>
        /// <para>Attempts to parse the specified string as an ISO-formatted DateTime. The formats supported are guided by ISO-8601, but do not match
        /// it exactly. Strings with no timezone information are parsed into DateTimeKind.Unspecified.</para>
        /// <para>ISO-8601 features not supported: day numbers; week numbers; time offsets; comma for decimal separation.</para>
        /// <para>Features supported not in ISO-8601: '-' separator for the basic format; date shortening; timezone marker for date-only strings.</para>
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

        /// <summary>
        /// Parse the specified string as an ISO-formatted DateTime. Returns null if the string is null or cannot be parsed.
        /// See <see cref="TryParseIso"/> for more info.
        /// </summary>
        public static DateTime? ParseIsoNullable(string str)
        {
            DateTime result;
            if (str == null || !TryParseIso(str, out result))
                return null;
            return result;
        }

        /// <summary>Returns a copy of this DateTime, truncated to whole milliseconds.</summary>
        public static DateTime TruncatedToMilliseconds(this DateTime datetime)
        {
            return new DateTime(datetime.Ticks - datetime.Ticks % TimeSpan.TicksPerMillisecond, datetime.Kind);
        }

        /// <summary>Returns a copy of this DateTime, truncated to whole seconds.</summary>
        public static DateTime TruncatedToSeconds(this DateTime datetime)
        {
            return new DateTime(datetime.Ticks - datetime.Ticks % TimeSpan.TicksPerSecond, datetime.Kind);
        }

        /// <summary>Returns a copy of this DateTime, truncated to whole minutes.</summary>
        public static DateTime TruncatedToMinutes(this DateTime datetime)
        {
            return new DateTime(datetime.Ticks - datetime.Ticks % TimeSpan.TicksPerMinute, datetime.Kind);
        }

        /// <summary>Returns a copy of this DateTime, truncated to whole days.</summary>
        public static DateTime TruncatedToDays(this DateTime datetime)
        {
            return new DateTime(datetime.Ticks - datetime.Ticks % TimeSpan.TicksPerDay, datetime.Kind);
        }
    }
}
