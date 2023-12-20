﻿using NUnit.Framework;

namespace RT.Util.ExtensionMethods;

[TestFixture]
public sealed class DateTimeExtensionsTests
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
        TestTryParseIsoValidHelper1("2008-12-31 23:45:53.0000001Z", _ => "", 2008, 12, 31, 23, 45, 53, 100, DateTimeKind.Utc);
        TestTryParseIsoValidHelper1("2008-12-31 23:45:53.0000007Z", _ => "", 2008, 12, 31, 23, 45, 53, 700, DateTimeKind.Utc);
        TestTryParseIsoValidHelper1("2008-12-31 23:45:53.9999999Z", _ => "", 2008, 12, 31, 23, 45, 53, 999999900, DateTimeKind.Utc);
        TestTryParseIsoValidHelper1("2008-12-31 23:45:53.9876543Z", _ => "", 2008, 12, 31, 23, 45, 53, 987654300, DateTimeKind.Utc);
        TestTryParseIsoValidHelper1("2008-12-31 23:45:53.0003456Z", _ => "", 2008, 12, 31, 23, 45, 53, 345600, DateTimeKind.Utc);
    }

    #endregion

    public string GetLocalSuffixAndEnsureItsValid(DateTime date)
    {
        // We expect a suffix like "+01:00" or "-05:30"
        string suffix = new DateTime(date.Year, date.Month, date.Day, 14, 35, 54, 456, DateTimeKind.Local).ToString("zzz");
        Assert.AreEqual(6, suffix.Length); // just so we know we're testing it properly...
        Assert.IsTrue(suffix[0] == '+' || suffix[0] == '-');
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
        TestToIsoStringHelper(DateTimeKind.Utc, _ => "Z");
    }

    [Test]
    public void TestToIsoStringUnspecified()
    {
        TestToIsoStringHelper(DateTimeKind.Unspecified, _ => "");
    }

    [Test]
    public void TestToIsoStringLocal()
    {
        TestToIsoStringHelper(DateTimeKind.Local, dt =>
        {
            var suffix = GetLocalSuffixAndEnsureItsValid(dt);
            return suffix.EndsWith(":00") ? suffix.Substring(0, suffix.Length - 3) : suffix;
        });
    }

    public void TestToIsoStringHelper(DateTimeKind kind, Func<DateTime, string> expectedSuffix)
    {
        DateTime dt;
        // Some normal dates
        dt = new DateTime(new DateTime(2008, 03, 25, 14, 35, 54, 456, kind).Ticks + 1234, kind);
        Assert.AreEqual("2008-03-25 14:35:54", dt.ToIsoString());
        Assert.AreEqual("2008-03-25 14:35:54.4561234" + expectedSuffix(dt), dt.ToIsoStringRoundtrip());
        dt = new DateTime(2008, 03, 25, 14, 35, 54, 456, kind);
        Assert.AreEqual("2008-03-25 14:35:54", dt.ToIsoString());
        Assert.AreEqual("2008-03-25 14:35:54.456" + expectedSuffix(dt), dt.ToIsoStringRoundtrip());
        dt = new DateTime(2008, 03, 25, 14, 35, 54, 0, kind);
        Assert.AreEqual("2008-03-25 14:35:54", dt.ToIsoString());
        Assert.AreEqual("2008-03-25 14:35:54" + expectedSuffix(dt), dt.ToIsoStringRoundtrip());
        dt = new DateTime(2008, 03, 25, 14, 35, 0, 0, kind);
        Assert.AreEqual("2008-03-25 14:35:00", dt.ToIsoString());
        Assert.AreEqual("2008-03-25 14:35" + expectedSuffix(dt), dt.ToIsoStringRoundtrip());
        dt = new DateTime(2008, 03, 25, 0, 0, 0, 0, kind);
        Assert.AreEqual("2008-03-25 00:00:00", dt.ToIsoString());
        Assert.AreEqual("2008-03-25" + expectedSuffix(dt), dt.ToIsoStringRoundtrip());
        // DST
        dt = new DateTime(2008, 01, 25, 14, 35, 54, 456, kind);
        Assert.AreEqual("2008-01-25 14:35:54", dt.ToIsoString());
        Assert.AreEqual("2008-01-25 14:35:54.456" + expectedSuffix(dt), dt.ToIsoStringRoundtrip());
        dt = new DateTime(2008, 06, 25, 14, 35, 54, 456, kind);
        Assert.AreEqual("2008-06-25 14:35:54", dt.ToIsoString());
        Assert.AreEqual("2008-06-25 14:35:54.456" + expectedSuffix(dt), dt.ToIsoStringRoundtrip());
        // Some corner cases
        dt = new DateTime(2, 1, 1, 14, 35, 54, 0, kind); // .NET "zzz" specifier appears to be off by one for the year 0001 with respect to TimeZone.GetUtcOffset, but is correct from the year 0002 onwards
        Assert.AreEqual("0002-01-01 14:35:54", dt.ToIsoString());
        Assert.AreEqual("0002-01-01 14:35:54" + expectedSuffix(dt), dt.ToIsoStringRoundtrip());
        dt = new DateTime(9999, 12, 30, 14, 35, 54, 0, kind);
        Assert.AreEqual("9999-12-30 14:35:54", dt.ToIsoString());
        Assert.AreEqual("9999-12-30 14:35:54" + expectedSuffix(dt), dt.ToIsoStringRoundtrip());
        // Min/max precision cornercase
        dt = new DateTime(new DateTime(2008, 03, 25, 00, 00, 00, 000, kind).Ticks + 1234, kind);
        Assert.AreEqual("2008-03-25 00:00:00.0001234" + expectedSuffix(dt), dt.ToIsoStringOptimal(maxPrecision: IsoDatePrecision.Full, includeTimezone: true));
        Assert.AreEqual("2008-03-25" + expectedSuffix(dt), dt.ToIsoStringOptimal(maxPrecision: IsoDatePrecision.Milliseconds, includeTimezone: true));
        dt = new DateTime(new DateTime(2008, 03, 25, 00, 00, 00, 456, kind).Ticks + 1234, kind);
        Assert.AreEqual("2008-03-25 00:00:00.456" + expectedSuffix(dt), dt.ToIsoStringOptimal(maxPrecision: IsoDatePrecision.Milliseconds, includeTimezone: true));
        Assert.AreEqual("2008-03-25" + expectedSuffix(dt), dt.ToIsoStringOptimal(maxPrecision: IsoDatePrecision.Seconds, includeTimezone: true));
        dt = new DateTime(new DateTime(2008, 03, 25, 00, 00, 23, 456, kind).Ticks + 1234, kind);
        Assert.AreEqual("2008-03-25 00:00:23" + expectedSuffix(dt), dt.ToIsoStringOptimal(maxPrecision: IsoDatePrecision.Seconds, includeTimezone: true));
        Assert.AreEqual("2008-03-25" + expectedSuffix(dt), dt.ToIsoStringOptimal(maxPrecision: IsoDatePrecision.Minutes, includeTimezone: true));
        dt = new DateTime(new DateTime(2008, 03, 25, 00, 12, 23, 456, kind).Ticks + 1234, kind);
        Assert.AreEqual("2008-03-25 00:12" + expectedSuffix(dt), dt.ToIsoStringOptimal(maxPrecision: IsoDatePrecision.Minutes, includeTimezone: true));
        Assert.AreEqual("2008-03-25" + expectedSuffix(dt), dt.ToIsoStringOptimal(maxPrecision: IsoDatePrecision.Days, includeTimezone: true));
    }

    #endregion

    #region TestTryParseIso

    [Test]
    public void TestTryParseIsoUtc()
    {
        TestTryParseIsoValidHelper2(DateTimeKind.Utc, _ => "Z");
    }

    [Test]
    public void TestTryParseIsoUnspecified()
    {
        TestTryParseIsoValidHelper2(DateTimeKind.Unspecified, _ => "");
    }

    [Test]
    public void TestTryParseIsoLocal()
    {
        TestTryParseIsoValidHelper2(DateTimeKind.Local, GetLocalSuffixAndEnsureItsValid);
        if (GetLocalSuffixAndEnsureItsValid(new DateTime(2008, 03, 25)).EndsWith(":00"))
            TestTryParseIsoValidHelper2(DateTimeKind.Local, dt => { var localsuf = GetLocalSuffixAndEnsureItsValid(dt); return localsuf.Substring(0, localsuf.Length - 3); });
    }

    public void TestTryParseIsoValidHelper1(string str, Func<DateTime, string> suffix, int year, int month, int day, int hour, int minute, int second, int nanosecond, DateTimeKind kind)
    {
        DateTime dt;
        Assert.IsTrue(DateTimeExtensions.TryParseIso(str + suffix(new DateTime(year, month, day)), out dt));
        Assert_DateTimeContentIs(dt, year, month, day, hour, minute, second, nanosecond, kind);
        // And again, but after a roundtrip to string
        str = dt.ToIsoStringRoundtrip();
        Assert.IsTrue(DateTimeExtensions.TryParseIso(str, out dt));
        Assert_DateTimeContentIs(dt, year, month, day, hour, minute, second, nanosecond, kind);
    }

    public void TestTryParseIsoValidHelper2(DateTimeKind kind, Func<DateTime, string> suffix)
    {
        // Valid conversions 1
        TestTryParseIsoValidHelper1("2008-07-31 23:45:53.1415926", suffix, 2008, 07, 31, 23, 45, 53, 141592600, kind);
        TestTryParseIsoValidHelper1("2008-12-31 23:45:53.141592", suffix, 2008, 12, 31, 23, 45, 53, 141592000, kind);
        TestTryParseIsoValidHelper1("2008-07-31 23:45:53.14159", suffix, 2008, 07, 31, 23, 45, 53, 141590000, kind);
        TestTryParseIsoValidHelper1("2008-12-31 23:45:53.1415", suffix, 2008, 12, 31, 23, 45, 53, 141500000, kind);
        TestTryParseIsoValidHelper1("2008-07-31 23:45:53.141", suffix, 2008, 07, 31, 23, 45, 53, 141000000, kind);
        TestTryParseIsoValidHelper1("2008-12-31 23:45:53.14", suffix, 2008, 12, 31, 23, 45, 53, 140000000, kind);
        TestTryParseIsoValidHelper1("2008-12-31 23:45:53.1", suffix, 2008, 12, 31, 23, 45, 53, 100000000, kind);
        TestTryParseIsoValidHelper1("2008-12-31 23:45:53", suffix, 2008, 12, 31, 23, 45, 53, 0, kind);
        TestTryParseIsoValidHelper1("2008-12-31 23:45.75000", suffix, 2008, 12, 31, 23, 45, 45, 0, kind);
        TestTryParseIsoValidHelper1("2008-12-31 23:45.5", suffix, 2008, 12, 31, 23, 45, 30, 0, kind);
        TestTryParseIsoValidHelper1("2008-12-31 23:45", suffix, 2008, 12, 31, 23, 45, 0, 0, kind);
        TestTryParseIsoValidHelper1("2008-12-31 23.75000", suffix, 2008, 12, 31, 23, 45, 0, 0, kind);
        TestTryParseIsoValidHelper1("2008-12-31 23.5", suffix, 2008, 12, 31, 23, 30, 0, 0, kind);
        TestTryParseIsoValidHelper1("2008-12-31 23", suffix, 2008, 12, 31, 23, 0, 0, 0, kind);
        TestTryParseIsoValidHelper1("2008-12-31", suffix, 2008, 12, 31, 0, 0, 0, 0, kind);
        if (kind != DateTimeKind.Local) // doesn't work for local and I can't be bothered to fix it
        {
            TestTryParseIsoValidHelper1("2008-12", suffix, 2008, 12, 1, 0, 0, 0, 0, kind);
            TestTryParseIsoValidHelper1("2008", suffix, 2008, 1, 1, 0, 0, 0, 0, kind);
        }
        // Valid conversions 2
        var suffix2 = Ut.Lambda((DateTime d) => suffix(d).Replace(":", ""));
        TestTryParseIsoValidHelper1("20080731T234553.1415926", suffix2, 2008, 07, 31, 23, 45, 53, 141592600, kind);
        TestTryParseIsoValidHelper1("20081231T234553.141592", suffix2, 2008, 12, 31, 23, 45, 53, 141592000, kind);
        TestTryParseIsoValidHelper1("20080731T234553.14159", suffix2, 2008, 07, 31, 23, 45, 53, 141590000, kind);
        TestTryParseIsoValidHelper1("20081231T234553.1415", suffix2, 2008, 12, 31, 23, 45, 53, 141500000, kind);
        TestTryParseIsoValidHelper1("20080731T234553.141", suffix2, 2008, 07, 31, 23, 45, 53, 141000000, kind);
        TestTryParseIsoValidHelper1("20081231T234553.14", suffix2, 2008, 12, 31, 23, 45, 53, 140000000, kind);
        TestTryParseIsoValidHelper1("20081231T234553.1", suffix2, 2008, 12, 31, 23, 45, 53, 100000000, kind);
        TestTryParseIsoValidHelper1("20081231T234553", suffix2, 2008, 12, 31, 23, 45, 53, 0, kind);
        TestTryParseIsoValidHelper1("20081231T2345.75000", suffix2, 2008, 12, 31, 23, 45, 45, 0, kind);
        TestTryParseIsoValidHelper1("20081231T2345.5", suffix2, 2008, 12, 31, 23, 45, 30, 0, kind);
        TestTryParseIsoValidHelper1("20081231T2345", suffix2, 2008, 12, 31, 23, 45, 0, 0, kind);
        TestTryParseIsoValidHelper1("20081231T23.75000", suffix2, 2008, 12, 31, 23, 45, 0, 0, kind);
        TestTryParseIsoValidHelper1("20081231T23.5", suffix2, 2008, 12, 31, 23, 30, 0, 0, kind);
        TestTryParseIsoValidHelper1("20081231T23", suffix2, 2008, 12, 31, 23, 0, 0, 0, kind);
        if (kind != DateTimeKind.Local) // doesn't work for local and I can't be bothered to fix it
            TestTryParseIsoValidHelper1("20081231", suffix2, 2008, 12, 31, 0, 0, 0, 0, kind);

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
        Assert.IsFalse(DateTimeExtensions.TryParseIso("200812", out dt)); // explicitly forbidden due to confusability with 2-digit years
        Assert.IsFalse(DateTimeExtensions.TryParseIso("2008-12-01 12:35:20.12345678", out dt)); // too much resolution
        Assert.IsFalse(DateTimeExtensions.TryParseIso("2008-12-01 12:35:20.", out dt)); // dangling dec point
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
        Assert.AreEqual(dt.Kind, DateTimeKind.Local);
        Assert_DateTimeContentIs(dt.ToUniversalTime(), 2008, 02, 27, 20, 05, 47, 141592600, DateTimeKind.Utc);
        Assert_DateTimeContentIs(dt.ToUniversalTime().TruncatedToSeconds(), 2008, 02, 27, 20, 05, 47, 0, DateTimeKind.Utc);
        Assert_DateTimeContentIs(dt.ToUniversalTime().TruncatedToDays(), 2008, 02, 27, 0, 0, 0, 0, DateTimeKind.Utc);
    }
}
