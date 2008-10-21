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
    }
}
