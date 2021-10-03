﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace RT.Util.ExtensionMethods
{
    [TestFixture]
    public sealed class StringExtensionsTests
    {
        [Test]
        public void TestRepeat()
        {
            Assert.AreEqual("", "xyz".Repeat(0));
            Assert.AreEqual("xyz", "xyz".Repeat(1));
            Assert.AreEqual("xyzxyz", "xyz".Repeat(2));
            Assert.AreEqual("xyzxyzxyzxyzxyzxyz", "xyz".Repeat(6));

            Assert.AreEqual("", "".Repeat(0));
            Assert.AreEqual("", "".Repeat(1));
            Assert.AreEqual("", "".Repeat(2));
            Assert.AreEqual("", "".Repeat(6));
        }

        [Test]
        public void TestEscape()
        {
            Assert.AreEqual("", "".HtmlEscape());
            Assert.AreEqual("One man&#39;s &quot;&lt;&quot; is another one&#39;s &quot;&gt;&quot;.", @"One man's ""<"" is another one's "">"".".HtmlEscape());
            Assert.AreEqual("One%20man%27s%20%22%3C%22%20is%20another%20one%27s%20%22%3E%22.", @"One man's ""<"" is another one's "">"".".UrlEscape());
            Assert.AreEqual(@"One man's ""<"" is another one's "">"".", "One%20man%27s%20%22%3C%22%20is%20another%20one%27s%20%22%3E%22.".UrlUnescape());
            Assert.AreEqual(@"'One man\'s ""\n"" is another one\'s ""\r\n"".'", "One man's \"\n\" is another one's \"\r\n\".".JsEscape(JsQuotes.Single));
            Assert.AreEqual(@"""One man's \""\n\"" is another one's \""\r\n\"".""", "One man's \"\n\" is another one's \"\r\n\".".JsEscape(JsQuotes.Double));
            Assert.AreEqual(@"One man\u0027s \""\n\"" is another one\u0027s \""\r\n\"".", "One man's \"\n\" is another one's \"\r\n\".".JsEscape(JsQuotes.None));

            Assert.AreEqual(2, "á".ToUtf8().Length);
            Assert.AreEqual(0xc3, "á".ToUtf8()[0]);
            Assert.AreEqual(0xa1, "á".ToUtf8()[1]);
            Assert.AreEqual(3, "語".ToUtf8().Length);
            Assert.AreEqual(0xe8, "語".ToUtf8()[0]);
            Assert.AreEqual(0xaa, "語".ToUtf8()[1]);
            Assert.AreEqual(0x9e, "語".ToUtf8()[2]);
        }

        [Test]
        public void TestCLiteralEscape()
        {
            AssertCLiteralEscape("", @"");
            AssertCLiteralEscape("test, прове́рка", @"test, прове́рка");
            AssertCLiteralEscape("\0\a\b\t\n\v\f\r\\", @"\0\a\b\t\n\v\f\r\\");
            AssertCLiteralEscape("test\r\n; \tstuff\x15\x1A", @"test\r\n; \tstuff\u0015\u001A");
            AssertCLiteralEscape("\"", @"\""");
            AssertCLiteralEscape(((char) 0xD812) + "123", @"\uD812123");
            Assert.AreEqual("test\\u000D\\u000A; \\u0009stuff\\u0015\\u001A -- \\u0041".CLiteralUnescape(), "test\\r\\n; \\tstuff\\u0015\\u001A -- A".CLiteralUnescape());
            try { @"test, \z stuff".CLiteralUnescape(); Assert.Fail(); }
            catch (ArgumentException e) { Assert.IsTrue(e.Message.Contains("6")); Assert.IsTrue(e.Message.Contains(@"\z")); }
            Assert.Throws<ArgumentException>(() => { @"test, \".CLiteralUnescape(); Assert.Fail(); });
            Assert.Throws<ArgumentException>(() => { @"test, \x".CLiteralUnescape(); Assert.Fail(); });
        }

        private void AssertCLiteralEscape(string unescaped, string expectEscaped)
        {
            var actualEscaped = unescaped.CLiteralEscape();
            var actualUnescaped = expectEscaped.CLiteralUnescape();
            Assert.AreEqual(expectEscaped, actualEscaped);
            Assert.AreEqual(unescaped, actualUnescaped);
        }

        [Test]
        public void TestFilenameEscape()
        {
            Assert.AreEqual("blah", "blah".FilenameCharactersEscape());
            Assert.AreEqual("bl{2F}ah", "bl/ah".FilenameCharactersEscape());
            Assert.AreEqual("bl{2A}{2A}ah", "bl**ah".FilenameCharactersEscape());
            Assert.AreEqual("{2A}", "*".FilenameCharactersEscape());

            Assert.AreEqual("blah ", "blah ".FilenameCharactersEscape());
            Assert.AreEqual("blah.txt.", "blah.txt.".FilenameCharactersEscape());

            Assert.AreEqual("blah", "blah".FilenameCharactersUnescape());
            Assert.AreEqual("bl/ah", "bl{2F}ah".FilenameCharactersUnescape());
            Assert.AreEqual("bl**ah", "bl{2A}{2A}ah".FilenameCharactersUnescape());
            Assert.AreEqual("*", "{2A}".FilenameCharactersUnescape());
            Assert.AreEqual("bl{*ah", "bl{{2A}ah".FilenameCharactersUnescape());

            Assert.AreEqual("bl{{}ah", "bl{{}ah".FilenameCharactersUnescape());
            Assert.AreEqual("bl}ah", "bl}ah".FilenameCharactersUnescape());
            Assert.AreEqual("bl{2A2A2A2A2A}ah", "bl{2A2A2A2A2A}ah".FilenameCharactersUnescape());
            Assert.AreEqual("bl{2AH", "bl{2AH".FilenameCharactersUnescape());
            Assert.AreEqual("bl{2A", "bl{2A".FilenameCharactersUnescape());
            Assert.AreEqual("bl{2", "bl{2".FilenameCharactersUnescape());
            Assert.AreEqual("bl{", "bl{".FilenameCharactersUnescape());
        }

        [Test]
        public void TestTrivial()
        {
            var tww = "\n\n\n".WordWrap(40).ToArray();

            Assert.AreEqual(4, tww.Length);
            Assert.AreEqual("", tww[0]);
            Assert.AreEqual("", tww[1]);
            Assert.AreEqual("", tww[2]);
            Assert.AreEqual("", tww[3]);
        }

        [Test]
        public void TestSingleNoIndentation()
        {
            string textSimple = "A delegate object is normally constructed by providing the name of the method the delegate will wrap, or with an anonymous Method. Once a delegate is instantiated, a method call made to the delegate will be passed by the delegate to that method. The parameters passed to the delegate by the caller are passed to the method, and the return value, if any, from the method is returned to the caller by the delegate.";

            var tww = textSimple.WordWrap(80).ToArray();
            Assert.AreEqual(6, tww.Length);
            Assert.AreEqual("A delegate object is normally constructed by providing the name of the method", tww[0]);
            Assert.AreEqual("the delegate will wrap, or with an anonymous Method. Once a delegate is", tww[1]);
            Assert.AreEqual("instantiated, a method call made to the delegate will be passed by the delegate", tww[2]);
            Assert.AreEqual("to that method. The parameters passed to the delegate by the caller are passed", tww[3]);
            Assert.AreEqual("to the method, and the return value, if any, from the method is returned to the", tww[4]);
            Assert.AreEqual("caller by the delegate.", tww[5]);

            tww = textSimple.WordWrap(40).ToArray();
            Assert.AreEqual(11, tww.Length);
            Assert.AreEqual("A delegate object is normally", tww[0]);
            Assert.AreEqual("constructed by providing the name of the", tww[1]);
            Assert.AreEqual("method the delegate will wrap, or with", tww[2]);
            Assert.AreEqual("an anonymous Method. Once a delegate is", tww[3]);
            Assert.AreEqual("instantiated, a method call made to the", tww[4]);
            Assert.AreEqual("delegate will be passed by the delegate", tww[5]);
            Assert.AreEqual("to that method. The parameters passed to", tww[6]);
            Assert.AreEqual("the delegate by the caller are passed to", tww[7]);
            Assert.AreEqual("the method, and the return value, if", tww[8]);
            Assert.AreEqual("any, from the method is returned to the", tww[9]);
            Assert.AreEqual("caller by the delegate.", tww[10]);
        }

        [Test]
        public void TestMultiIndentedParagraphs()
        {
            string textComplex = "   Delegate types    are derived from the       Delegate class in the .NET Framework. Delegate     types are sealed - they cannot be derived from - and it is not     possible to derive custom classes from Delegate.\n\n      Because the       instantiated delegate is an object, it can be      passed as a parameter, or assigned to a property. This allows a method to accept        a delegate as a parameter, and call the delegate at some later time.\r\n Para with windows line break and a single space indentation.";
            var tww = textComplex.WordWrap(60).ToArray();
            Assert.AreEqual(11, tww.Length);
            Assert.AreEqual("   Delegate types are derived from the Delegate class in the", tww[0]);
            Assert.AreEqual("   .NET Framework. Delegate types are sealed - they cannot", tww[1]);
            Assert.AreEqual("   be derived from - and it is not possible to derive custom", tww[2]);
            Assert.AreEqual("   classes from Delegate.", tww[3]);
            Assert.AreEqual("", tww[4]);
            Assert.AreEqual("      Because the instantiated delegate is an object, it can", tww[5]);
            Assert.AreEqual("      be passed as a parameter, or assigned to a property.", tww[6]);
            Assert.AreEqual("      This allows a method to accept a delegate as a", tww[7]);
            Assert.AreEqual("      parameter, and call the delegate at some later time.", tww[8]);
            Assert.AreEqual(" Para with windows line break and a single space", tww[9]);
            Assert.AreEqual(" indentation.", tww[10]);
        }

        private void assertBase64UrlArray(byte[] arr)
        {
            string b64u = arr.Base64UrlEncode();
            string b64u_check = Convert.ToBase64String(arr).Replace('+', '-').Replace('/', '_').Replace("=", "");
            Assert.AreEqual(b64u, b64u_check);

            byte[] dec = b64u.Base64UrlDecode();
            Assert.AreEqual(arr, dec);
        }

        [Test]
        public void TestBase64Url()
        {
            assertBase64UrlArray(new byte[] { });

            for (int i = 0; i < 256; i++)
                assertBase64UrlArray(new byte[] { (byte) i });

            for (byte i = 5; i < 200; i += 61) // 5, 66, 127, 188
                for (int j = 0; j < 256; j++)
                    assertBase64UrlArray(new byte[] { i, (byte) j });

            for (byte i = 5; i < 200; i += 61)
                for (int j = 0; j < 256; j++)
                    assertBase64UrlArray(new byte[] { i, i, (byte) j });

            for (byte i = 5; i < 200; i += 61)
                for (int j = 0; j < 256; j++)
                    assertBase64UrlArray(new byte[] { i, i, i, (byte) j });

            for (byte i = 5; i < 200; i += 61)
                for (int j = 0; j < 256; j++)
                    assertBase64UrlArray(new byte[] { i, i, i, i, (byte) j });

            for (byte i = 5; i < 200; i += 61)
                for (int j = 0; j < 256; j++)
                    assertBase64UrlArray(new byte[] { i, i, (byte) j, i, i, (byte) j });

            for (byte i = 5; i < 200; i += 61)
                for (int j = 0; j < 256; j++)
                    assertBase64UrlArray(new byte[] { i, i, i, (byte) j, i, i, (byte) j });

            for (byte i = 5; i < 200; i += 61)
                for (int j = 0; j < 256; j++)
                    assertBase64UrlArray(new byte[] { i, (byte) j, i, i, i, i, i, (byte) j });
        }

        [Test]
        public void TestStringReplace()
        {
            Assert.Throws<ArgumentNullException>(() => StringExtensions.Replace(null, null, null, StringComparison.CurrentCulture));
            Assert.Throws<ArgumentNullException>(() => StringExtensions.Replace(null, "X", "", StringComparison.CurrentCulture));
            Assert.Throws<ArgumentNullException>(() => StringExtensions.Replace("", null, "", StringComparison.CurrentCulture));
            Assert.Throws<ArgumentNullException>(() => StringExtensions.Replace("", "X", null, StringComparison.CurrentCulture));
            Assert.Throws<ArgumentException>(() => StringExtensions.Replace("", "", "", StringComparison.CurrentCulture));

            Assert.AreEqual("", "".Replace("a", "b", StringComparison.OrdinalIgnoreCase));
            Assert.AreEqual("b", "a".Replace("a", "b", StringComparison.OrdinalIgnoreCase));
            Assert.AreEqual("b", "A".Replace("a", "b", StringComparison.OrdinalIgnoreCase));
            Assert.AreEqual("Once Upon A Time There Was A Beau!!iful Princess.", "Once Upon A Time There Was A Beautiful Princess.".Replace("t", "!!", StringComparison.Ordinal));
            Assert.AreEqual("But She Had An !!chantm!!t Upon Her Of A Fearful Sort,", "But She Had An Enchantment Upon Her Of A Fearful Sort,".Replace("en", "!!", StringComparison.OrdinalIgnoreCase));
            Assert.AreEqual("Which Could Only Be Broken By Love’s First Kiss.", "Which Could Only Be Broken By Love’s First Kiss.".Replace("X", "Y", StringComparison.Ordinal));
        }

        [Test]
        public void TestFmt()
        {
            assertFmts("5", "5", "{0}", 5);
            assertFmts("{", "{", "{{");
            assertFmts("}", "}", "}}", "dummy");
            assertFmts("{", null, "{");
            assertFmts("}", null, "}");
            assertFmts("", "", "");
            assertFmts("thingy3stuff2blah", "thingy3stuff2blah", "thingy{2}stuff{1}blah", 1, 2, 3);
            assertFmts("=1,000,000,000=", "=1,000,000,000=", "={0:#,0}=", 1e9);
            assertFmts("=11{12yy}=", null, "={0:dd{MM}yy}=", new DateTime(2013, 12, 11));
            assertFmts("=11{12}13}=", "=11{12}13}=", "={0:dd{{MM}}yy}}}=", new DateTime(2013, 12, 11));
            assertFmts("={x}=", "={x}=", "={{{0}}}=", "x");
        }

        private void assertFmts(string expectedFmt, string expectedFormat, string format, params object[] args)
        {
            string resultFmt, resultFormat;
            var culture = CultureInfo.InvariantCulture;

            if (expectedFmt != null && expectedFormat != null && expectedFmt != expectedFormat)
                throw new Exception("Bug in Fmt tests");

            try { resultFmt = format.Fmt(culture, args); }
            catch { resultFmt = null; }

            try { resultFormat = string.Format(culture, format, args); }
            catch { resultFormat = null; }

            Assert.AreEqual(expectedFmt == null, resultFmt == null);
            Assert.AreEqual(expectedFormat == null, resultFormat == null);
            if (expectedFmt != null)
                Assert.AreEqual(expectedFmt, resultFmt);
            if (expectedFormat != null)
                Assert.AreEqual(expectedFormat, resultFormat);
        }

        [Test]
        public void TestSubstrings()
        {
            for (int f = -3; f <= 3; f += 3)
                Assert.AreEqual("", "".SubstringSafe(f));
            for (int f = -3; f <= 3; f += 3)
                for (int l = -3; l <= 3; l += 3)
                    Assert.AreEqual("", "".SubstringSafe(f, l));

            Assert.AreEqual("foobar", "foobar".SubstringSafe(-1));
            Assert.AreEqual("foobar", "foobar".SubstringSafe(0));
            Assert.AreEqual("oobar", "foobar".SubstringSafe(1));
            Assert.AreEqual("r", "foobar".SubstringSafe(5));
            Assert.AreEqual("", "foobar".SubstringSafe(6));
            Assert.AreEqual("", "foobar".SubstringSafe(7));

            Assert.AreEqual("fo", "foobar".SubstringSafe(-1, 3));
            Assert.AreEqual("foo", "foobar".SubstringSafe(0, 3));
            Assert.AreEqual("oob", "foobar".SubstringSafe(1, 3));
            Assert.AreEqual("r", "foobar".SubstringSafe(5, 3));
            Assert.AreEqual("", "foobar".SubstringSafe(6, 3));
            Assert.AreEqual("", "foobar".SubstringSafe(7, 3));
            Assert.AreEqual("", "foobar".SubstringSafe(2, 0));
            Assert.AreEqual("", "foobar".SubstringSafe(2, -1));

            assertSubstringRight("foobar", "foobar", -1, true);
            assertSubstringRight("foobar", "foobar", 0, false);
            assertSubstringRight("foobar", "fooba", 1, false);
            assertSubstringRight("foobar", "f", 5, false);
            assertSubstringRight("foobar", "", 6, false);
            assertSubstringRight("foobar", "", 7, true);

            for (int f = -3; f <= 3; f += 3)
                assertSubstringRight("", "", f, f != 0);
            for (int f = -3; f <= 3; f += 3)
                for (int l = -3; l <= 3; l += 3)
                    assertSubstringRight("", "", f, l, f != 0 || l != 0);

            assertSubstringRight("foobar", "ar", -1, 3, true);
            assertSubstringRight("foobar", "bar", 0, 3, false);
            assertSubstringRight("foobar", "oba", 1, 3, false);
            assertSubstringRight("foobar", "foo", 3, 3, false);
            assertSubstringRight("foobar", "fo", 4, 3, true);
            assertSubstringRight("foobar", "f", 5, 3, true);
            assertSubstringRight("foobar", "", 6, 3, true);
            assertSubstringRight("foobar", "", 7, 3, true);
            assertSubstringRight("foobar", "", -1, -3, true);
            assertSubstringRight("foobar", "", 2, -3, true);
            assertSubstringRight("foobar", "", 8, -3, true);
        }

        private void assertSubstringRight(string str, string expected, int startIndex, bool outOfRange)
        {
            var exp = new string(new string(str.Reverse().ToArray()).SubstringSafe(startIndex).Reverse().ToArray());
            Assert.AreEqual(exp, expected);
            Assert.AreEqual(expected, str.SubstringRightSafe(startIndex));
            if (outOfRange)
                try { str.SubstringRight(startIndex); Assert.Fail(); } // Assert.Throws counts as unhandled in user code and stops in the debugger...
                catch (ArgumentOutOfRangeException) { }
            else
                Assert.AreEqual(expected, str.SubstringRight(startIndex));
        }

        private void assertSubstringRight(string str, string expected, int startIndex, int length, bool outOfRange)
        {
            var exp = new string(new string(str.Reverse().ToArray()).SubstringSafe(startIndex, length).Reverse().ToArray());
            Assert.AreEqual(exp, expected);
            Assert.AreEqual(expected, str.SubstringRightSafe(startIndex, length));
            if (outOfRange)
                try { str.SubstringRight(startIndex, length); Assert.Fail(); } // Assert.Throws counts as unhandled in user code and stops in the debugger...
                catch (ArgumentOutOfRangeException) { }
            else
                Assert.AreEqual(expected, str.SubstringRight(startIndex, length));
        }
    }
}
