using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace RT.Util.ExtensionMethods
{
    [TestFixture]
    public class StringExtensionsTests
    {
        public void Assert_Join(string separator, IEnumerable<string> values, string expected)
        {
            Assert.AreEqual(expected, separator.Join(values));
            Assert.AreEqual(expected, values.Join(separator));
        }

        [Test]
        public void TestJoin()
        {
            Assert_Join(", ", new[] {"London", "Paris", "Tokyo"}, "London, Paris, Tokyo");
            Assert_Join("|", new[] { "London", "Paris", "Tokyo" }, "London|Paris|Tokyo");

            Assert_Join("|", new string[] {}, "");
            Assert_Join("|", new[] { "London" }, "London");
        }
    }
}
