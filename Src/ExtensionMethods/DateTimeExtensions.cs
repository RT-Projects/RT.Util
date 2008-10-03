using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

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
            return ((int)(datetime.Ticks % 10000000)) * 100;
        }

        #region ISO 8601 conversion

        /// <summary>
        /// Lists all the datetime formats acceptable when converting a string containing a UTC
        /// datetime to a DateTime structure. The first three formats are also used for converting
        /// the other way.
        /// </summary>
        private static readonly string[] datetimeFormatsUtc =
            {
                // The most likely formats should be near the top for speed reasons
                "yyyy-MM-dd HH:mm:ss.fffffffZ", // also used for to-full & to-optimal
                "yyyy-MM-dd HH:mm:ssZ",         // also used for to-optimal
                "yyyy-MM-ddZ",                  // also used for to-optimal
                // The following strings are only here to enable conversion from
                // the many possible formats defined in the ISO.
                "yyyy-MM-dd HH:mm:ss.ffffffZ",
                "yyyy-MM-dd HH:mm:ss.fffffZ",
                "yyyy-MM-dd HH:mm:ss.ffffZ",
                "yyyy-MM-dd HH:mm:ss.fffZ",
                "yyyy-MM-dd HH:mm:ss.ffZ",
                "yyyy-MM-dd HH:mm:ss.fZ",
                "yyyyMMddTHHmmss.fffffffZ",
                "yyyyMMddTHHmmss.ffffffZ",
                "yyyyMMddTHHmmss.fffffZ",
                "yyyyMMddTHHmmss.ffffZ",
                "yyyyMMddTHHmmss.fffZ",
                "yyyyMMddTHHmmss.ffZ",
                "yyyyMMddTHHmmss.fZ",
                "yyyyMMddTHHmmssZ",
                "yyyyMMddZ",
                // The even less common strings: their presense does not
                // affect the speed at which the common strings get parsed.
                "yyyy-MM-dd HH:mmZ",
                "yyyy-MM-dd HHZ",
                "yyyyMMddTHHmmZ",
                "yyyyMMddTHHZ",
            };

        /// <summary>
        /// Lists all the datetime formats acceptable when converting a string containing a Local
        /// datetime to a DateTime structure. The first three formats are also used for converting
        /// the other way.
        /// </summary>
        private static string[] datetimeFormatsLocal =
            {
                // The most likely formats should be near the top for speed reasons
                "yyyy-MM-dd HH:mm:ss.fffffffzzz", // also used for to-full & to-optimal
                "yyyy-MM-dd HH:mm:sszzz",         // also used for to-optimal
                "yyyy-MM-ddzzz",                  // also used for to-optimal
                // The following strings are only here to enable conversion from
                // the many possible formats defined in the ISO.
                "yyyy-MM-dd HH:mm:ss.ffffffzzz",
                "yyyy-MM-dd HH:mm:ss.fffffzzz",
                "yyyy-MM-dd HH:mm:ss.ffffzzz",
                "yyyy-MM-dd HH:mm:ss.fffzzz",
                "yyyy-MM-dd HH:mm:ss.ffzzz",
                "yyyy-MM-dd HH:mm:ss.fzzz",
                "yyyyMMddTHHmmss.fffffffzzz",
                "yyyyMMddTHHmmss.ffffffzzz",
                "yyyyMMddTHHmmss.fffffzzz",
                "yyyyMMddTHHmmss.ffffzzz",
                "yyyyMMddTHHmmss.fffzzz",
                "yyyyMMddTHHmmss.ffzzz",
                "yyyyMMddTHHmmss.fzzz",
                "yyyyMMddTHHmmsszzz",
                "yyyyMMddzzz",
                // The even less common strings: their presense does not
                // affect the speed at which the common strings get parsed.
                "yyyyMMddTHHmmzzz",
                "yyyyMMddTHHzzz",
                "yyyy-MM-dd HH:mmzzz",
                "yyyy-MM-dd HHzzz",
            };

        /// <summary>
        /// Lists all the datetime formats acceptable when converting a string containing an Unspecified
        /// datetime to a DateTime structure. The first three formats are also used for converting
        /// the other way.
        /// </summary>
        private static string[] datetimeFormatsUnspecified =
            {
                // The most likely formats should be near the top for speed reasons
                "yyyy-MM-dd HH:mm:ss.fffffff", // also used for to-full & to-optimal
                "yyyy-MM-dd HH:mm:ss",         // also used for to-optimal
                "yyyy-MM-dd",                  // also used for to-optimal
                // The following strings are only here to enable conversion from
                // the many possible formats defined in the ISO.
                "yyyy-MM-dd HH:mm:ss.ffffff",
                "yyyy-MM-dd HH:mm:ss.fffff",
                "yyyy-MM-dd HH:mm:ss.ffff",
                "yyyy-MM-dd HH:mm:ss.fff",
                "yyyy-MM-dd HH:mm:ss.ff",
                "yyyy-MM-dd HH:mm:ss.f",
                "yyyyMMddTHHmmss.fffffff",
                "yyyyMMddTHHmmss.ffffff",
                "yyyyMMddTHHmmss.fffff",
                "yyyyMMddTHHmmss.ffff",
                "yyyyMMddTHHmmss.fff",
                "yyyyMMddTHHmmss.ff",
                "yyyyMMddTHHmmss.f",
                "yyyyMMddTHHmmss",
                "yyyyMMdd",
                // The even less common strings: their presense does not
                // affect the speed at which the common strings get parsed.
                "yyyy-MM-dd HH:mm",
                "yyyy-MM-dd HH",
                "yyyyMMddTHHmm",
                "yyyyMMddTHH",
            };

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
                    return datetime.ToString(datetimeFormatsUtc[0]);
                case DateTimeKind.Local:
                    return datetime.ToString(datetimeFormatsLocal[0]);
                case DateTimeKind.Unspecified:
                    return datetime.ToString(datetimeFormatsUnspecified[0]);
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
        /// 
        /// Example 1: "2007-12-31 21:00:00Z" - the nanoseconds are all 0
        /// Example 2: "2007-12-31Z" - the hours, minutes, seconds, nanoseconds are all 0
        /// 
        /// Note that in the first example the ISO format allows the minutes and seconds
        /// to be skipped as well - this is not implemented at the moment because the
        /// resulting string looks too ambiguous and hard to interpret.
        /// </summary>
        public static string ToIsoStringOptimal(this DateTime datetime)
        {
            int formatIndex = 2;
            if (datetime.Nanosecond() != 0)
                formatIndex = 0;
            else if (datetime.Second != 0 || datetime.Minute != 0 || datetime.Hour != 0)
                formatIndex = 1;

            switch (datetime.Kind)
            {
                case DateTimeKind.Utc:
                    return datetime.ToString(datetimeFormatsUtc[formatIndex]);
                case DateTimeKind.Local:
                    return datetime.ToString(datetimeFormatsLocal[formatIndex]);
                case DateTimeKind.Unspecified:
                    return datetime.ToString(datetimeFormatsUnspecified[formatIndex]);
                default:
                    // to keep the compiler happy
                    throw new Exception("Unexpected DateTime.Kind");
            }
        }

        /// <summary>
        /// Attempts to parse the specified string as an ISO-formatted DateTime. Note
        /// that only a subset of string formats defined by the ISO 8601 is supported,
        /// specifically (by example):
        /// 
        /// 2008-12-31 22:15:56.1234567         20081231T221556.1234567
        /// ...
        /// 2008-12-31 22:15:56.1               20081231T221556.1
        /// 2008-12-31 22:15:56                 20081231T221556
        /// 2008-12-31 22:15                    20081231T2215
        /// 2008-12-31 22                       20081231T22
        /// 2008-12-31                          20081231
        /// 
        /// plus all of the above with the suffix "Z" (to signify UTC time) or a suffix
        /// like "+01:30" (to signify a local time at the specified offset from UTC time).
        /// Without the suffix a string is parsed into a DateTimeKind.Unspecified kind of date.
        /// </summary>
        public static bool TryParseIso(string str, out DateTime result)
        {
            if (DateTime.TryParseExact(str, datetimeFormatsUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out result))
                return true;
            else if (DateTime.TryParseExact(str, datetimeFormatsLocal, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out result))
                return true;
            else if (DateTime.TryParseExact(str, datetimeFormatsUnspecified, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out result))
                return true;
            else
                return false;
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
