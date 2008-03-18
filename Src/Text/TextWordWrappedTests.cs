using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace RT.Util.Text
{
    [TestFixture]
    public class TextWordWrappedTests
    {
        string textSimple = "A delegate object is normally constructed by providing the name of the method the delegate will wrap, or with an anonymous Method. Once a delegate is instantiated, a method call made to the delegate will be passed by the delegate to that method. The parameters passed to the delegate by the caller are passed to the method, and the return value, if any, from the method is returned to the caller by the delegate.";
        string textComplex = "   Delegate types    are derived from the       Delegate class in the .NET Framework. Delegate     types are sealed - they cannot be derived from - and it is not     possible to derive custom classes from Delegate.\n\n      Because the       instantiated delegate is an object, it can be      passed as a parameter, or assigned to a property. This allows a method to accept        a delegate as a parameter, and call the delegate at some later time.\r\n Para with windows line break and a single space indentation.";

        [Test]
        public void TestTrivial()
        {
            TextWordWrapped tww;

            tww = new TextWordWrapped("\n\n\n", 40);
            Assert.AreEqual("", tww[0]);
            Assert.AreEqual("", tww[1]);
            Assert.AreEqual("", tww[2]);
            VerifyIndexer(tww);
        }

        [Test]
        public void TestSingleNoIndentation()
        {
            TextWordWrapped tww;

            tww = new TextWordWrapped(textSimple, 80);
            Assert.AreEqual("A delegate object is normally constructed by providing the name of the method",   tww[0]);
            Assert.AreEqual("the delegate will wrap, or with an anonymous Method. Once a delegate is",         tww[1]);
            Assert.AreEqual("instantiated, a method call made to the delegate will be passed by the delegate", tww[2]);
            Assert.AreEqual("to that method. The parameters passed to the delegate by the caller are passed",  tww[3]);
            Assert.AreEqual("to the method, and the return value, if any, from the method is returned to the", tww[4]);
            Assert.AreEqual("caller by the delegate.", tww[5]);
            VerifyIndexer(tww);

            tww = new TextWordWrapped(textSimple, 40);
            Assert.AreEqual("A delegate object is normally", tww[0]);
            Assert.AreEqual("constructed by providing the name of the", tww[1]);
            Assert.AreEqual("method the delegate will wrap, or with",   tww[2]);
            Assert.AreEqual("an anonymous Method. Once a delegate is",  tww[3]);
            Assert.AreEqual("instantiated, a method call made to the",  tww[4]);
            Assert.AreEqual("delegate will be passed by the delegate",  tww[5]);
            Assert.AreEqual("to that method. The parameters passed to", tww[6]);
            Assert.AreEqual("the delegate by the caller are passed to", tww[7]);
            Assert.AreEqual("the method, and the return value, if",     tww[8]);
            Assert.AreEqual("any, from the method is returned to the",  tww[9]);
            Assert.AreEqual("caller by the delegate.", tww[10]);
            VerifyIndexer(tww);
        }

        [Test]
        public void TestMultiIndentedParagraphs()
        {
            TextWordWrapped tww;

            tww = new TextWordWrapped(textComplex, 60);
            Assert.AreEqual("   Delegate types are derived from the Delegate class in the", tww[0]);
            Assert.AreEqual("   .NET Framework. Delegate types are sealed - they cannot",   tww[1]);
            Assert.AreEqual("   be derived from - and it is not possible to derive custom", tww[2]);
            Assert.AreEqual("   classes from Delegate.", tww[3]);
            Assert.AreEqual("", tww[4]);
            Assert.AreEqual("      Because the instantiated delegate is an object, it can", tww[5]);
            Assert.AreEqual("      be passed as a parameter, or assigned to a property.",   tww[6]);
            Assert.AreEqual("      This allows a method to accept a delegate as a",         tww[7]);
            Assert.AreEqual("      parameter, and call the delegate at some later time.",   tww[8]);
            Assert.AreEqual(" Para with windows line break and a single space",             tww[9]);
            Assert.AreEqual(" indentation.", tww[10]);
            VerifyIndexer(tww);
        }

        private void VerifyIndexer(TextWordWrapped tww)
        {
            for (int i = 0; i < tww.Lines.Count; i++)
                Assert.AreEqual(tww.Lines[i], tww[i]);

            for (int i = tww.Lines.Count; i < tww.Lines.Count + 20; i++)
                Assert.AreEqual("", tww[i]);
        }
    }
}
