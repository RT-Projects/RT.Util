using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using RT.Util.ExtensionMethods;

namespace RT.Util.ExtensionMethods
{
    [TestFixture]
    public class DateTimeExtensionsTests
    {
        private void Assert_DateTimeContentIs(DateTime dt, int year, int month, int day, int hour, int minute, int second, int nanosecond, DateTimeKind kind)
        {
            Assert.AreEqual(year, dt.Year);
            Assert.AreEqual(month, dt.Month);
            Assert.AreEqual(day, dt.Day);
            Assert.AreEqual(hour, dt.Hour);
            Assert.AreEqual(minute, dt.Minute);
            Assert.AreEqual(second, dt.Second);
            Assert.AreEqual(nanosecond / 1000000, dt.Millisecond);
            Assert.AreEqual(nanosecond, dt.Nanosecond());
            Assert.AreEqual(kind, dt.Kind);
        }

        #region TestGetNanoseconds

        [Test]
        public void TestGetNanoseconds()
        {
            TestTryParseIsoValidHelper1("2008-12-31 23:45:53.0000001Z", 2008, 12, 31, 23, 45, 53, 100, DateTimeKind.Utc);
            TestTryParseIsoValidHelper1("2008-12-31 23:45:53.0000007Z", 2008, 12, 31, 23, 45, 53, 700, DateTimeKind.Utc);
            TestTryParseIsoValidHelper1("2008-12-31 23:45:53.9999999Z", 2008, 12, 31, 23, 45, 53, 999999900, DateTimeKind.Utc);
            TestTryParseIsoValidHelper1("2008-12-31 23:45:53.9876543Z", 2008, 12, 31, 23, 45, 53, 987654300, DateTimeKind.Utc);
            TestTryParseIsoValidHelper1("2008-12-31 23:45:53.0003456Z", 2008, 12, 31, 23, 45, 53, 345600, DateTimeKind.Utc);
        }

        #endregion

        public string GetLocalSuffixAndEnsureItsValid()
        {
            string suffix = DateTime.Now.ToLocalTime().ToString("zzz");
            Assert.AreEqual(6, suffix.Length); // just so we know we're testing it properly...
            Assert.AreEqual('+', suffix[0]);
            Assert.IsTrue(char.IsDigit(suffix[1]));
            Assert.IsTrue(char.IsDigit(suffix[2]));
            Assert.AreEqual(':', suffix[3]);
            Assert.IsTrue(char.IsDigit(suffix[4]));
            Assert.IsTrue(char.IsDigit(suffix[5]));
            return suffix;
        }

        #region TestToIsoString

        [Test]
        public void TestToIsoStringUtc()
        {
            TestToIsoStringHelper(DateTimeKind.Utc, "Z");
        }

        [Test]
        public void TestToIsoStringUnspecified()
        {
            TestToIsoStringHelper(DateTimeKind.Unspecified, "");
        }

        [Test]
        public void TestToIsoStringLocal()
        {
            TestToIsoStringHelper(DateTimeKind.Local, GetLocalSuffixAndEnsureItsValid());
        }

        public void TestToIsoStringHelper(DateTimeKind kind, string expectedSuffix)
        {
            DateTime dt;
            // Some normal dates
            dt = new DateTime(2008, 03, 25, 14, 35, 54, 456, kind);
            Assert.AreEqual("2008-03-25 14:35:54.4560000" + expectedSuffix, dt.ToIsoStringFull());
            Assert.AreEqual("2008-03-25 14:35:54.4560000" + expectedSuffix, dt.ToIsoStringOptimal());
            dt = new DateTime(2008, 03, 25, 14, 35, 54, 0, kind);
            Assert.AreEqual("2008-03-25 14:35:54.0000000" + expectedSuffix, dt.ToIsoStringFull());
            Assert.AreEqual("2008-03-25 14:35:54" + expectedSuffix, dt.ToIsoStringOptimal());
            dt = new DateTime(2008, 03, 25, 0, 0, 0, 0, kind);
            Assert.AreEqual("2008-03-25 00:00:00.0000000" + expectedSuffix, dt.ToIsoStringFull());
            Assert.AreEqual("2008-03-25" + expectedSuffix, dt.ToIsoStringOptimal());
            // Some corner cases
            dt = new DateTime(1, 1, 1, 14, 35, 54, 0, kind);
            Assert.AreEqual("0001-01-01 14:35:54.0000000" + expectedSuffix, dt.ToIsoStringFull());
            Assert.AreEqual("0001-01-01 14:35:54" + expectedSuffix, dt.ToIsoStringOptimal());
            dt = new DateTime(9999, 12, 30, 14, 35, 54, 0, kind);
            Assert.AreEqual("9999-12-30 14:35:54.0000000" + expectedSuffix, dt.ToIsoStringFull());
            Assert.AreEqual("9999-12-30 14:35:54" + expectedSuffix, dt.ToIsoStringOptimal());
        }

        #endregion

        #region TestTryParseIso

        [Test]
        public void TestTryParseIsoUtc()
        {
            TestTryParseIsoValidHelper2(DateTimeKind.Utc, "Z");
        }

        [Test]
        public void TestTryParseIsoUnspecified()
        {
            TestTryParseIsoValidHelper2(DateTimeKind.Unspecified, "");
        }

        [Test]
        public void TestTryParseIsoLocal()
        {
            TestTryParseIsoValidHelper2(DateTimeKind.Local, GetLocalSuffixAndEnsureItsValid());
        }

        public void TestTryParseIsoValidHelper1(string str, int year, int month, int day, int hour, int minute, int second, int nanosecond, DateTimeKind kind)
        {
            DateTime dt;
            Assert.IsTrue(DateTimeExtensions.TryParseIso(str, out dt));
            Assert_DateTimeContentIs(dt, year, month, day, hour, minute, second, nanosecond, kind);
        }

        public void TestTryParseIsoValidHelper2(DateTimeKind kind, string suffix)
        {
            // Valid conversions 1
            TestTryParseIsoValidHelper1("2008-12-31 23:45:53.1415926" + suffix, 2008, 12, 31, 23, 45, 53, 141592600, kind);
            TestTryParseIsoValidHelper1("2008-12-31 23:45:53.141592" + suffix, 2008, 12, 31, 23, 45, 53, 141592000, kind);
            TestTryParseIsoValidHelper1("2008-12-31 23:45:53.14159" + suffix, 2008, 12, 31, 23, 45, 53, 141590000, kind);
            TestTryParseIsoValidHelper1("2008-12-31 23:45:53.1415" + suffix, 2008, 12, 31, 23, 45, 53, 141500000, kind);
            TestTryParseIsoValidHelper1("2008-12-31 23:45:53.141" + suffix, 2008, 12, 31, 23, 45, 53, 141000000, kind);
            TestTryParseIsoValidHelper1("2008-12-31 23:45:53.14" + suffix, 2008, 12, 31, 23, 45, 53, 140000000, kind);
            TestTryParseIsoValidHelper1("2008-12-31 23:45:53.1" + suffix, 2008, 12, 31, 23, 45, 53, 100000000, kind);
            TestTryParseIsoValidHelper1("2008-12-31 23:45:53" + suffix, 2008, 12, 31, 23, 45, 53, 0, kind);
            TestTryParseIsoValidHelper1("2008-12-31 23:45" + suffix, 2008, 12, 31, 23, 45, 0, 0, kind);
            TestTryParseIsoValidHelper1("2008-12-31 23" + suffix, 2008, 12, 31, 23, 0, 0, 0, kind);
            TestTryParseIsoValidHelper1("2008-12-31" + suffix, 2008, 12, 31, 0, 0, 0, 0, kind);
            // Valid conversions 2
            TestTryParseIsoValidHelper1("20081231T234553.1415926" + suffix, 2008, 12, 31, 23, 45, 53, 141592600, kind);
            TestTryParseIsoValidHelper1("20081231T234553.141592" + suffix, 2008, 12, 31, 23, 45, 53, 141592000, kind);
            TestTryParseIsoValidHelper1("20081231T234553.14159" + suffix, 2008, 12, 31, 23, 45, 53, 141590000, kind);
            TestTryParseIsoValidHelper1("20081231T234553.1415" + suffix, 2008, 12, 31, 23, 45, 53, 141500000, kind);
            TestTryParseIsoValidHelper1("20081231T234553.141" + suffix, 2008, 12, 31, 23, 45, 53, 141000000, kind);
            TestTryParseIsoValidHelper1("20081231T234553.14" + suffix, 2008, 12, 31, 23, 45, 53, 140000000, kind);
            TestTryParseIsoValidHelper1("20081231T234553.1" + suffix, 2008, 12, 31, 23, 45, 53, 100000000, kind);
            TestTryParseIsoValidHelper1("20081231T234553" + suffix, 2008, 12, 31, 23, 45, 53, 0, kind);
            TestTryParseIsoValidHelper1("20081231T2345" + suffix, 2008, 12, 31, 23, 45, 0, 0, kind);
            TestTryParseIsoValidHelper1("20081231T23" + suffix, 2008, 12, 31, 23, 0, 0, 0, kind);
            TestTryParseIsoValidHelper1("20081231" + suffix, 2008, 12, 31, 0, 0, 0, 0, kind);

            // Invalid conversions... not quite sure how to test these but something
            // is definitely better than nothing in this case.
            DateTime dt;
            // Garbage
            Assert.IsFalse(DateTimeExtensions.TryParseIso("", out dt));
            Assert.IsFalse(DateTimeExtensions.TryParseIso("25", out dt));
            Assert.IsFalse(DateTimeExtensions.TryParseIso("hfalfh", out dt));
            // Valid dates in non-ISO formats
            Assert.IsFalse(DateTimeExtensions.TryParseIso("31-12-2008", out dt));
            Assert.IsFalse(DateTimeExtensions.TryParseIso("31/12/2008", out dt));
            Assert.IsFalse(DateTimeExtensions.TryParseIso("12/31/2008", out dt));
            Assert.IsFalse(DateTimeExtensions.TryParseIso("1.1.2008", out dt));
            Assert.IsFalse(DateTimeExtensions.TryParseIso("1/1/2008", out dt));
            Assert.IsFalse(DateTimeExtensions.TryParseIso("1 January 2008", out dt));
            // Invalid dates in ISO formats
            Assert.IsFalse(DateTimeExtensions.TryParseIso("2008-12-32", out dt));
            Assert.IsFalse(DateTimeExtensions.TryParseIso("2008-13-31", out dt));
            Assert.IsFalse(DateTimeExtensions.TryParseIso("2008-01-01 25:00:00", out dt));
            Assert.IsFalse(DateTimeExtensions.TryParseIso("2008-01-01 12:61:00", out dt));
            Assert.IsFalse(DateTimeExtensions.TryParseIso("2008-01-01 12:30:61", out dt));
        }

        #endregion

        [Test]
        public void TestTruncated()
        {
            DateTime dt;

            DateTimeExtensions.TryParseIso("2008-02-27 21:35:47.1415926", out dt);
            Assert_DateTimeContentIs(dt, 2008, 02, 27, 21, 35, 47, 141592600, DateTimeKind.Unspecified);
            Assert_DateTimeContentIs(dt.TruncatedToSeconds(), 2008, 02, 27, 21, 35, 47, 0, DateTimeKind.Unspecified);
            Assert_DateTimeContentIs(dt.TruncatedToDays(), 2008, 02, 27, 0, 0, 0, 0, DateTimeKind.Unspecified);

            DateTimeExtensions.TryParseIso("2008-02-27 21:35:47.1415926Z", out dt);
            Assert_DateTimeContentIs(dt, 2008, 02, 27, 21, 35, 47, 141592600, DateTimeKind.Utc);
            Assert_DateTimeContentIs(dt.TruncatedToSeconds(), 2008, 02, 27, 21, 35, 47, 0, DateTimeKind.Utc);
            Assert_DateTimeContentIs(dt.TruncatedToDays(), 2008, 02, 27, 0, 0, 0, 0, DateTimeKind.Utc);

            DateTimeExtensions.TryParseIso("2008-02-27 21:35:47.1415926+01:30", out dt);
            Assert_DateTimeContentIs(dt, 2008, 02, 27, 20, 05, 47, 141592600, DateTimeKind.Local);
            Assert_DateTimeContentIs(dt.TruncatedToSeconds(), 2008, 02, 27, 20, 05, 47, 0, DateTimeKind.Local);
            Assert_DateTimeContentIs(dt.TruncatedToDays(), 2008, 02, 27, 0, 0, 0, 0, DateTimeKind.Local);
        }
    }
}
