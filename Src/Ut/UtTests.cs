using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace RT.Util
{
    [TestFixture]
    public class UtTests
    {
        [Test]
        public void TestRange()
        {
            Assert.AreEqual("12345678910", string.Join("", Enumerable.Range(1, 10).Select(i => i.ToString()).ToArray()));
            Assert.AreEqual(47, Enumerable.Range(47, 1).First());
            try
            {
                Enumerable.Range(1, 0).First();
                Assert.Fail("Exception expected");
            }
            catch (Exception) { }
        }
    }
}
