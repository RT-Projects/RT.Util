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
            Assert_Join(", ", new[] { "London", "Paris", "Tokyo" }, "London, Paris, Tokyo");
            Assert_Join("|", new[] { "London", "Paris", "Tokyo" }, "London|Paris|Tokyo");

            Assert_Join("|", new string[] { }, "");
            Assert_Join("|", new[] { "London" }, "London");
        }

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
            Assert.AreEqual("", "".HTMLEscape());
            Assert.AreEqual("One man&#39;s &quot;&lt;&quot; is another one&#39;s &quot;&gt;&quot;.", @"One man's ""<"" is another one's "">"".".HTMLEscape());
            Assert.AreEqual("One%20man's%20%22%3C%22%20is%20another%20one's%20%22%3E%22.", @"One man's ""<"" is another one's "">"".".URLEscape());
            Assert.AreEqual(@"One man's ""<"" is another one's "">"".", "One%20man's%20%22%3C%22%20is%20another%20one's%20%22%3E%22.".URLUnescape());
            Assert.AreEqual(@"""One man's \""<\"" is another one's \"">\"".\n""", "One man's \"<\" is another one's \">\".\n".JSEscape());
            
            Assert.AreEqual(2, "á".ToUTF8().Length);
            Assert.AreEqual(0xc3, "á".ToUTF8()[0]);
            Assert.AreEqual(0xa1, "á".ToUTF8()[1]);
            Assert.AreEqual(3, "語".ToUTF8().Length);
            Assert.AreEqual(0xe8, "語".ToUTF8()[0]);
            Assert.AreEqual(0xaa, "語".ToUTF8()[1]);
            Assert.AreEqual(0x9e, "語".ToUTF8()[2]);
        }

        public enum TestEnum { First, Second, Third };
        [Test]
        public void TestStaticValue()
        {
            Assert.AreEqual(TestEnum.First, "First".ToStaticValue(typeof(TestEnum)));
            Assert.AreEqual(TestEnum.Second, "Second".ToStaticValue(typeof(TestEnum)));
            Assert.AreEqual(TestEnum.Third, "Third".ToStaticValue(typeof(TestEnum)));
            Assert.AreEqual(TestEnum.First, "First".ToStaticValue<TestEnum>());
            Assert.AreEqual(TestEnum.Second, "Second".ToStaticValue<TestEnum>());
            Assert.AreEqual(TestEnum.Third, "Third".ToStaticValue<TestEnum>());

            try
            {
                "Fourth".ToStaticValue<TestEnum>();
                Assert.Fail("Exception expected");
            }
            catch (Exception) { }
        }
    }
}
