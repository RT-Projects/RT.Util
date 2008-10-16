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
            Assert.AreEqual("12345678910", string.Join("", Ut.Range(1, 10).Select(i => i.ToString()).ToArray()));
            Assert.AreEqual(47, Ut.Range(47, 47).First());
            try
            {
                Ut.Range(1, 0).First();
                Assert.Fail("Exception expected");
            }
            catch (Exception) { }
        }
    }
}
