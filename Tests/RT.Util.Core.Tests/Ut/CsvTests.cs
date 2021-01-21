using System;
using System.Linq;
using NUnit.Framework;

namespace RT.Util
{
    [TestFixture]
    public sealed class CsvTests
    {
        [Test]
        public void CsvTest()
        {
            check("", 0);
            check("", 4);
            check("a", 0, new[] { "a" });
            check("a", 4, new[] { "a", "", "", "" });
            check("a,b,c,d,e", 0, new[] { "a", "b", "c", "d", "e" });
            check("a,b,c,d,e", 2, new[] { "a", "b", "c", "d", "e" });
            check("a,b,c,d,e", 7, new[] { "a", "b", "c", "d", "e", "", "" });
            check("a,", 0, new[] { "a", "" });
            check("a,,", 0, new[] { "a", "", "" });
            check(",a", 0, new[] { "", "a" });
            check(",,a", 0, new[] { "", "", "a" });
            check(",", 0, new[] { "", "" });
            check(",,", 0, new[] { "", "", "" });
            check("a,b\r\nc\r\n d,e", 0, new[] { "a", "b" }, new[] { "c" }, new[] { " d", "e" });
            check("a,b\nc\n d,e", 0, new[] { "a", "b" }, new[] { "c" }, new[] { " d", "e" });
            check("\r\n\r\na,b\r\n\r\nc\r\n\r\n d,e\r\n", 0, new[] { "a", "b" }, new[] { "c" }, new[] { " d", "e" });
            check("\r\n\r\na,b\r\n\r\nc\r\n,\r\n d,e\r\n", 0, new[] { "a", "b" }, new[] { "c" }, new[] { "", "" }, new[] { " d", "e" });
            check(" a  ,   b    ,     c      ", 0, new[] { " a  ", "   b    ", "     c      " });
            check("\r\n a  ,   b    ,     c      \r\n", 0, new[] { " a  ", "   b    ", "     c      " });
            check("\r\n", 0);
            check("\r\n \n", 0, new[] { " " });
            check("a\r\nb\n\n\nc", 0, new[] { "a" }, new[] { "b" }, new[] { "c" });
            check("\"\"", 0, new[] { "" });
            check("\"a\"", 0, new[] { "a" });
            check("a,\"b\",c,d,e", 0, new[] { "a", "b", "c", "d", "e" });
            check("\" foo \"\"bar\"\", stuff \"", 0, new[] { " foo \"bar\", stuff " });
            Assert.Throws<InvalidOperationException>(() => Ut.ParseCsv("\"foo\"bar").ToArray());
        }

        private void check(string rawCsv, int minColumns, params string[][] expected)
        {
            var parsed = Ut.ParseCsv(rawCsv, minColumns).ToArray();
            Assert.AreEqual(expected.Length, parsed.Length);
            for (int r = 0; r < parsed.Length; r++)
            {
                Assert.AreEqual(expected[r].Length, parsed[r].Length);
                for (int c = 0; c < parsed[r].Length; c++)
                    Assert.AreEqual(expected[r][c], parsed[r][c]);
            }
        }
    }
}
